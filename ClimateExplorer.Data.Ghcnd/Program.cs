using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ClimateExplorer.Core.Enums;
using ClimateExplorer.Data.Ghcnd;

var serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(x => { x.SingleLine = true; x.IncludeScopes = false; })
        )
    .BuildServiceProvider();
var logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger<Program>();

var httpClient = new HttpClient();
var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

var stations = await Station.GetStationsFromFile(@"MetaData\selected-stations.json");

var nullRecord = "9999";

Directory.CreateDirectory("Download");

foreach (var station in stations)
{
    var csvFilePathAndName = @$"Download\{station.Id}.csv";

    if (File.Exists(csvFilePathAndName))
    {
        logger.LogInformation($"GHCNd file for {station.Id} ({station.Name}) already exists ({csvFilePathAndName}). Will not download it again.");
    }
    else
    {
        logger.LogInformation($"Downloading GHCNd for {station.Id}");
        var url = $"https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/{station.Id}.csv";
        var response = await httpClient.GetAsync(url);

        var content = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync(csvFilePathAndName, content);
        logger.LogInformation($"Downloaded GHCNd for {station.Id} ({station.Name}) in {station.CountryCode}");
    }
}



var stationsWithData = new List<Station>();
var outputFolder = @"Output\Data\";

try
{
    Directory.Delete(outputFolder, true);
}
catch { }
Directory.CreateDirectory(outputFolder);

Parallel.ForEach(stations, station =>
{
    var dataFileFilterAndAdjustments = new List<DataFileFilterAndAdjustment>
    {
        new DataFileFilterAndAdjustment
        {
            Id = station.Id
        }
    };

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null,
    };

    try
    {
        var csvFilePathAndName = @$"Download\{station.Id}.csv";

        if (File.Exists(csvFilePathAndName))
        {
            logger.LogInformation($"Reading {csvFilePathAndName}.");

            using var reader = new StreamReader(csvFilePathAndName);
            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<GhcndInputRow>().ToList();

            if (records.Any())
            {
                var outputFile = $@"{outputFolder}{station.Id}.csv";

                var cleanedRecords = records
                                .Where(x => !string.IsNullOrWhiteSpace(x.Prcp) && !string.IsNullOrWhiteSpace(x.Tmax) && !string.IsNullOrWhiteSpace(x.Tmin))
                                .ToList();

                var sufficientData = SufficientData(cleanedRecords);

                if (!sufficientData)
                {
                    logger.LogInformation($"Insufficient data exists for {station.Id}. It will be excluded from the dataset.");
                    return;
                }

                using (var writer = new StreamWriter(outputFile))
                using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    var nicerRecords = cleanedRecords.Select(x => new OutputRow
                    {
                        Date = x.Date?.Replace("-", string.Empty),
                        Precipitation = string.IsNullOrWhiteSpace(x.Prcp) ? nullRecord : x.Prcp.Trim('\"').Trim(),
                        Tmax = string.IsNullOrWhiteSpace(x.Tmax) ? nullRecord : x.Tmax.Trim('\"').Trim(),
                        Tmin = string.IsNullOrWhiteSpace(x.Tmin) ? nullRecord : x.Tmin.Trim('\"').Trim()
                    });

                    logger.LogInformation($"Writing {outputFile}.");
                    csvWriter.WriteRecords(nicerRecords);
                }

                stationsWithData.Add(station);
            }
            else
            {
                logger.LogInformation($"No data has been found for {station.Id}. It will be excluded from the dataset.");
            }

        }
    }
    catch (BadDataException ex)
    {
        logger.LogError($"File for station {station.Id} has bad data. Will skip this station.", ex);
    }
    catch (FileLoadException ex)
    {
        logger.LogError($"Error loading file for station {station.Id}. Will skip this station.", ex);
    }
});

bool SufficientData(List<GhcndInputRow> cleanedRecords)
{
    var yearRecords = new Dictionary<int, int>();
    foreach (var record in cleanedRecords)
    {
        var year = int.Parse(record.Date!.Substring(0, 4));
        if (yearRecords.ContainsKey(year))
        {
            yearRecords[year]++;
        }
        else
        {
            yearRecords.Add(year, 1);
        }
    }
    // Need at least 10 years worth of data where each of those years have more than 300 days of data.
    var yearsWithMinimumNumberOfRecords = yearRecords.Values.Where(x => x > 300);
    return yearsWithMinimumNumberOfRecords.Count() > 10;
}

var dataFileMapping = new DataFileMapping
{
    DataSetDefinitionId = Guid.Parse("87C65C34-C689-4BA1-8061-626E4A63D401"),
    LocationIdToDataFileMappings = new Dictionary<Guid, List<DataFileFilterAndAdjustment>>()
};

var ghcnIdToLocationIds = await GetGhcnIdToLocationIds(stations);

stationsWithData.ForEach(x =>
{
    dataFileMapping.LocationIdToDataFileMappings.Add(
        ghcnIdToLocationIds[x.Id],
        [
                new()
                {
                    Id = x.Id
                }
        ]);
});


var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

Directory.CreateDirectory(@"Output\DataFileMapping");

var outputFileSuffix = "_ghcnd";

File.WriteAllText($@"Output\DataFileMapping\DataFileMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileMapping, jsonSerializerOptions));

static async Task<Dictionary<string, Guid>> GetGhcnIdToLocationIds(List<Station> stations)
{
    const string ghcnIdToLocationIdsFile = @"MetaData\GhcnIdToLocationIds.json";
    Dictionary<string, Guid>? ghcnIdToLocationIds = null;
    if (File.Exists(ghcnIdToLocationIdsFile))
    {
        var contents = await File.ReadAllTextAsync(ghcnIdToLocationIdsFile);
        ghcnIdToLocationIds = JsonSerializer.Deserialize<Dictionary<string, Guid>>(contents);
        return ghcnIdToLocationIds!;
    }

    throw new Exception($"Expecting {ghcnIdToLocationIdsFile} to exist");
}