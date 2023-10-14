using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ClimateExplorer.Core.Enums;

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

Directory.CreateDirectory("Download");

foreach (var station in stations)
{
    var csvFilePathAndName = @$"Download\{station.Id}.csv";

    if (File.Exists(csvFilePathAndName))
    {
        logger.LogInformation($"Precipitation file for {station.Id} ({station.Name}) already exists ({csvFilePathAndName}). Will not download it again.");
    }
    else
    {
        var url = $"https://www.ncei.noaa.gov/data/ghcnm/v4beta/access/{station.Id}.csv";
        var response = await httpClient.GetAsync(url);

        var content = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync(csvFilePathAndName, content);
        logger.LogInformation($"Downloaded precipitation for {station.Id} ({station.Name}) in {station.CountryCode}");
    }
}



var measurementDefinition = new MeasurementDefinition
{
    DataAdjustment = null,
    DataType = DataType.Rainfall,
    DataResolution = DataResolution.Monthly,
    NullValue = "99999",
    DataRowRegEx = @"^(?<station>\w+),.*,.*,.*,.*,(?<year>\d{4})(?<month>\d{2}),\s*(?<value>-?[\d+]*),.*,.*,\w,\d+$",
    FolderName = @"Download",
    FileNameFormat = "[station].csv",
};

var stationsWithData = new List<Station>();

var outputFolder = @"Output\Data\";

Directory.Delete(outputFolder, true);
Directory.CreateDirectory(outputFolder);

StreamWriter? writer = null;

const int minimumYearsOfData = 10;

foreach (var station in stations)
{
    var dataFileFilterAndAdjustments = new List<DataFileFilterAndAdjustment>
    {
        new DataFileFilterAndAdjustment
        {
            Id = station.Id
        }
    };

    List<DataRecord>? dataRecords = null;
    try
    {
        dataRecords = await DataReader.GetDataRecords(measurementDefinition, dataFileFilterAndAdjustments);
    }
    catch (FileLoadException ex)
    {
        logger.LogError($"Error loading file for station {station.Id}. Will skip this station.", ex);
        continue;
    }
    
    if (dataRecords!.Any())
    {
        logger.LogInformation($"Precipitation data has been loaded for {station.Id}. There are {dataRecords.Count} records. Will now save to a simplified file format into the output folder.");

        var fileName = $@"{outputFolder}{station.Id}.csv";
        if (File.Exists(fileName))
        {
            throw new Exception($"File {fileName} exists already");
        }
        logger.LogInformation($"Creating data file for station {station.Id}");
        writer = File.CreateText(fileName);

        var year = dataRecords[0].Year;
        var month = 1;
        var values = new float[12];
        var yearsOfData = 0;

        foreach (var record in dataRecords)
        {
            if (record.Year < year)
            {
                continue;
            }
            if (record.Year != year || record.Month! != month || record.Value == null)
            {
                logger.LogError($"{station.Id}: Missing data for year {year}. Will skip this year.");
                year++;
                month = 1;
                values = new float[12];
                continue;
            }
            else
            {
                values[month - 1] = record.Value!.Value == -1 ? 0 : record.Value!.Value;

                if (month == 12)
                {
                    logger.LogInformation($"{station.Id} has a complete set of values for year {year}. Will write this year to the output file.");
                    writer!.WriteLine($"{year},{string.Join(',', values)}");

                    year++;
                    month = 1;
                    values = new float[12];

                    yearsOfData++;
                }
                else
                {
                    month++;
                }
            }
        }

        writer!.Close();

        if (yearsOfData < minimumYearsOfData)
        {
            logger.LogWarning($"{station.Id}: station only has {yearsOfData} years of data. We require at least {minimumYearsOfData} years of data. Station will be excluded from the dataset.");
            File.Delete(fileName);
        }
        else
        {
            stationsWithData.Add(station);
        }
    }
    else
    {
        logger.LogInformation($"No precipitation data has been found for {station.Id}. It will be excluded from the dataset.");
    }
}

var dataFileLocationMapping = new DataFileLocationMapping
{
    DataSetDefinitionId = Guid.Parse("6ABB028A-29F6-481C-837E-1FC9C8E989AF"),
    LocationIdToDataFileMappings = new Dictionary<Guid, List<DataFileFilterAndAdjustment>>()
};

var ghcnIdToLocationIds = await GetGhcnIdToLocationIds(stations);

stationsWithData.ForEach(x =>
{
    dataFileLocationMapping.LocationIdToDataFileMappings.Add(
        ghcnIdToLocationIds[x.Id],
        new List<DataFileFilterAndAdjustment>
        {
                new DataFileFilterAndAdjustment
                {
                    Id = x.Id
                }
        });
});


var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters =
        {
            new JsonStringEnumConverter()
        }
};

Directory.CreateDirectory(@"Output\Location");
Directory.CreateDirectory(@"Output\Station");
Directory.CreateDirectory(@"Output\DataFileLocationMapping");

var outputFileSuffix = "_ghcnm_precipitation";

File.WriteAllText($@"Output\DataFileLocationMapping\DataFileLocationMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileLocationMapping, jsonSerializerOptions));

static async Task<Dictionary<string, Guid>> GetGhcnIdToLocationIds(List<Station> stations)
{
    const string ghcnIdToLocationIdsFile = @"MetaData\GhcnIdToLocationIds.json";
    Dictionary<string, Guid>? ghcnIdToLocationIds = null;
    if (File.Exists(ghcnIdToLocationIdsFile))
    {
        var contents = await File.ReadAllTextAsync(ghcnIdToLocationIdsFile);
        ghcnIdToLocationIds = JsonSerializer.Deserialize<Dictionary<string, Guid>>(contents);
    }
    else
    {
        throw new Exception($"Expecting {ghcnIdToLocationIdsFile} to exist");
    }

    return ghcnIdToLocationIds!;
}