using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnd;
using CsvHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;

var serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(x => { x.SingleLine = true; x.IncludeScopes = false; })
        )
    .BuildServiceProvider();
var logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger<Program>();

var httpClient = GhcndHttpClientFactory.CreateHttpClient();

// Get the stations that were selected in the GHCNm project
var stations = await Station.GetStationsFromFile(Folders.SelectedStationsFile);

const string downloadFolder = "Download";
await GhcndBulkStationFileCache.DownloadStationsAsync(stations, httpClient, downloadFolder, logger);

var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();
var ghcndFolder = dataSetDefinitions.Single(x => x.ShortName == "GHCNd").MeasurementDefinitions!.First().FolderName!;
var ghcndpFolder = dataSetDefinitions.Single(x => x.ShortName == "GHCNdp").MeasurementDefinitions!.Single().FolderName!;

var stationsWithTempData = new List<Station>();
var stationsWithPrcpData = new List<Station>();
var temperatureFolder = Folders.SourceDataFolder + ghcndFolder;
var precipitationFolder = Folders.SourceDataFolder + ghcndpFolder;

try
{
    Directory.Delete(temperatureFolder, true);
    Directory.Delete(precipitationFolder, true);
}
catch { }
Directory.CreateDirectory(temperatureFolder);
Directory.CreateDirectory(precipitationFolder);

Parallel.ForEach(stations, station =>
{
    try
    {
        var csvFilePathAndName = GhcndBulkStationFileCache.GetCsvFilePathAndName(downloadFolder, station.Id);

        if (!File.Exists(csvFilePathAndName))
        {
            return;
        }

        logger.LogInformation($"Reading {csvFilePathAndName}.");

        var csvContent = File.ReadAllText(csvFilePathAndName);
        var rows = GhcndCsvReader.ReadRows(csvContent);

        if (rows.Count == 0)
        {
            logger.LogInformation($"No data has been found for {station.Id}. It will be excluded from the dataset.");
            return;
        }

        var rowsWithData = GhcndCsvReader.RemoveRowsWithNoData(rows);

        ProcessTemperature(station, rowsWithData);
        ProcessPrecipitation(station, rowsWithData);
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

await GhcndDataFileMappingBuilder.CreateDataFileMapping([.. stationsWithTempData.OrderBy(x => x.Id)], "temperature", "87C65C34-C689-4BA1-8061-626E4A63D401");
await GhcndDataFileMappingBuilder.CreateDataFileMapping([.. stationsWithPrcpData.OrderBy(x => x.Id)], "precipitation", "5BBEAF4C-B459-410E-9B77-470905CB1E46");

void ProcessTemperature(Station station, List<GhcndInputRow> rows)
{
    var temperatureRecords = GhcndTemperatureProcessor.CreateRecords(rows);
    GhcndTemperatureProcessor.ValidateRecords(temperatureRecords, station.Id, logger);
    var cleanedRecords = GhcndTemperatureProcessor.FilterSufficientData(temperatureRecords);

    if (cleanedRecords.Count < 10)
    {
        logger.LogInformation($"Insufficient temperature data exists for {station.Id}. It will be excluded from the dataset.");
        return;
    }

    WriteCsv($@"{temperatureFolder}\{station.Id}.csv", cleanedRecords);
    stationsWithTempData.Add(station);
}

void ProcessPrecipitation(Station station, List<GhcndInputRow> rows)
{
    var precipitationRecords = GhcndPrecipitationProcessor.CreateRecords(rows);
    GhcndPrecipitationProcessor.ValidateRecords(precipitationRecords, station.Id, logger);
    var cleanedRecords = GhcndPrecipitationProcessor.FilterSufficientData(precipitationRecords);

    if (cleanedRecords.Count < 10)
    {
        logger.LogInformation($"Insufficient precipitation data exists for {station.Id}. It will be excluded from the dataset.");
        return;
    }

    WriteCsv($@"{precipitationFolder}\{station.Id}.csv", cleanedRecords);
    stationsWithPrcpData.Add(station);
}

void WriteCsv<T>(string filePathAndName, IEnumerable<T> records)
{
    logger.LogInformation($"Writing {filePathAndName}.");
    using var writer = new StreamWriter(filePathAndName);
    using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
    csvWriter.WriteRecords(records);
}
