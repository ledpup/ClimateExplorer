using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
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



var stationsWithTempData = new List<Station>();
var stationsWithPrcpData = new List<Station>();
var outputFolder = @"Output\Data\";

try
{
    Directory.Delete(outputFolder, true);
}
catch { }
Directory.CreateDirectory(outputFolder + "Temperature");
Directory.CreateDirectory(outputFolder + "Precipitation");

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
                var recordsWithNoNullRows = records
                                .Where(x => !((string.IsNullOrWhiteSpace(x.Prcp) || x.Prcp == nullRecord)
                                            && (string.IsNullOrWhiteSpace(x.Tmax) || x.Tmax == nullRecord)
                                            && (string.IsNullOrWhiteSpace(x.Tmin) || x.Tmin == nullRecord)))
                                .ToList();

                var temperatureRecords = recordsWithNoNullRows.Select(x => new OutputRowTemperature
                {
                    Date = x.Date?.Replace("-", string.Empty),
                    Tmax = string.IsNullOrWhiteSpace(x.Tmax) ? nullRecord : x.Tmax.Trim('\"').Trim(),
                    Tmin = string.IsNullOrWhiteSpace(x.Tmin) ? nullRecord : x.Tmin.Trim('\"').Trim(),
                });

                var cleanedRecords = SufficientDataTemp(temperatureRecords);
                if (cleanedRecords.Count() < 10)
                {
                    logger.LogInformation($"Insufficient temperature data exists for {station.Id}. It will be excluded from the dataset.");
                }
                else
                {
                    var outputFileTemp = $@"{outputFolder}\Temp\{station.Id}.csv";
                    using (var writer = new StreamWriter(outputFileTemp))
                    using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        logger.LogInformation($"Writing {outputFileTemp}.");
                        csvWriter.WriteRecords(cleanedRecords);
                    }

                    stationsWithTempData.Add(station);
                }



                var prcpRecords = recordsWithNoNullRows.Select(x => new OutputRowPrecipitation
                {
                    Date = x.Date?.Replace("-", string.Empty),
                    Precipitation = string.IsNullOrWhiteSpace(x.Prcp) ? nullRecord : x.Prcp.Trim('\"').Trim(),
                });

                var cleanedRecordsPrcp = SufficientDataPrcp(prcpRecords);
                if (cleanedRecordsPrcp.Count() < 10)
                {
                    logger.LogInformation($"Insufficient precipitation data exists for {station.Id}. It will be excluded from the dataset.");
                }
                else
                {
                    var outputFilePrcp = $@"{outputFolder}\Prcp\{station.Id}.csv";
                    using (var writer = new StreamWriter(outputFilePrcp))
                    using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        logger.LogInformation($"Writing {outputFilePrcp}.");
                        csvWriter.WriteRecords(cleanedRecordsPrcp);
                    }

                    stationsWithPrcpData.Add(station);
                }
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


await CreateDataFileMapping(stations, stationsWithTempData, "temperature", "87C65C34-C689-4BA1-8061-626E4A63D401");
await CreateDataFileMapping(stations, stationsWithPrcpData, "precipitation", "5BBEAF4C-B459-410E-9B77-470905CB1E46");

IEnumerable<OutputRowTemperature> SufficientDataTemp(IEnumerable<OutputRowTemperature> records)
{
    var yearRecordCount = new Dictionary<string, TempRow>();
    foreach (var record in records)
    {
        var year = record.Date!.Substring(0, 4);
        if (!yearRecordCount.ContainsKey(year))
        {
            yearRecordCount.Add(year, new TempRow());
        }

        yearRecordCount[year].TMax += record.Tmax == nullRecord ? 0 : 1;
        yearRecordCount[year].TMin += record.Tmin == nullRecord ? 0 : 1;
    }

    var yearsWithMinimumNumberOfRecords = yearRecordCount.Where(x => x.Value.TMax > 300 && x.Value.TMin > 300).ToDictionary().Keys.ToList();

    var cleanedData = new List<OutputRowTemperature>();
    foreach (var record in records)
    {
        if (yearsWithMinimumNumberOfRecords.Contains(record.Date!.Substring(0, 4)))
        {
            cleanedData.Add(record);
        }
    }

    return cleanedData;
}

IEnumerable<OutputRowPrecipitation> SufficientDataPrcp(IEnumerable<OutputRowPrecipitation> records)
{
    var yearRecordCount = new Dictionary<string, int>();
    foreach (var record in records)
    {
        var year = record.Date!.Substring(0, 4);
        if (!yearRecordCount.ContainsKey(year))
        {
            yearRecordCount.Add(year, 0);
        }

        yearRecordCount[year] += record.Precipitation == nullRecord ? 0 : 1;
    }

    var yearsWithMinimumNumberOfRecords = yearRecordCount.Where(x => x.Value > 300).ToDictionary().Keys.ToList();

    var cleanedData = new List<OutputRowPrecipitation>();
    foreach (var record in records)
    {
        if (yearsWithMinimumNumberOfRecords.Contains(record.Date!.Substring(0, 4)))
        {
            cleanedData.Add(record);
        }
    }

    return cleanedData;
}

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

static async Task CreateDataFileMapping(List<Station> stations, List<Station> stationsWithData, string dataType, string dsdId)
{
    var dataFileMapping = new DataFileMapping
    {
        DataSetDefinitionId = Guid.Parse(dsdId),
        LocationIdToDataFileMappings = []
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

    File.WriteAllText($@"Output\DataFileMapping\DataFileMapping_ghcnd_{dataType}.json", JsonSerializer.Serialize(dataFileMapping, jsonSerializerOptions));
}

public record TempRow
{
    public int TMax { get; set; }
    public int TMin { get; set; }
}