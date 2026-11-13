using System.Collections.Concurrent;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnd;
using CsvHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
logger.LogInformation($"{stations.Count} stations selected in {Folders.SelectedStationsFile}.");

const string downloadFolder = "Download";
var notInGhcnd = await GhcndBulkStationFileCache.DownloadStationsAsync(stations, httpClient, downloadFolder, logger);

// One archive per station, GHCNd\[station].zip, holding Temperature\[station].csv and/or Precipitation\[station].csv.
// It's the same layout the Web API's runtime refresh writes, and DataPipeline copies these archives as they are.
var ghcndFolder = Path.GetFullPath(Path.Combine(Folders.SourceDataFolder, "GHCNd"));
Directory.CreateDirectory(ghcndFolder);

var oldArchives = Directory.GetFiles(ghcndFolder, "*.zip");
foreach (var oldArchive in oldArchives)
{
    File.Delete(oldArchive);
}

logger.LogInformation($"Deleted {oldArchives.Length} old station archives from {ghcndFolder}.");

// Older versions of this importer wrote split CSV folders, which DataPipeline doesn't package.
foreach (var legacyFolder in new[] { "Temperature", "Precipitation" }.Select(x => Path.Combine(ghcndFolder, x)).Where(Directory.Exists))
{
    Directory.Delete(legacyFolder, true);
    logger.LogInformation($"Deleted legacy folder {legacyFolder}.");
}

var stationsWithTempData = new ConcurrentBag<Station>();
var stationsWithPrcpData = new ConcurrentBag<Station>();
var stationsWithNoUsableData = new ConcurrentBag<string>();
var stationsWithBadFiles = new ConcurrentBag<string>();

await Parallel.ForEachAsync(stations, async (station, cancellationToken) =>
{
    try
    {
        var csvFilePathAndName = GhcndBulkStationFileCache.GetCsvFilePathAndName(downloadFolder, station.Id);

        if (!File.Exists(csvFilePathAndName))
        {
            return;
        }

        logger.LogInformation($"Reading {csvFilePathAndName}.");

        var csvContent = await File.ReadAllTextAsync(csvFilePathAndName, cancellationToken);
        var rows = GhcndCsvReader.ReadRows(csvContent);

        if (rows.Count == 0)
        {
            logger.LogInformation($"No data has been found for {station.Id}. It will be excluded from the dataset.");
            stationsWithNoUsableData.Add(station.Id);
            return;
        }

        var rowsWithData = GhcndCsvReader.RemoveRowsWithNoData(rows);

        var temperatureRecords = GetTemperatureRecords(station, rowsWithData);
        var precipitationRecords = GetPrecipitationRecords(station, rowsWithData);

        if (temperatureRecords == null && precipitationRecords == null)
        {
            stationsWithNoUsableData.Add(station.Id);
            return;
        }

        var archivePath = Path.Combine(ghcndFolder, $"{station.Id}.zip");
        await GhcndStationArchiveBuilder.WriteAsync(archivePath, station.Id, temperatureRecords, precipitationRecords, cancellationToken);
        logger.LogInformation($"Wrote {archivePath} ({Describe(temperatureRecords, precipitationRecords)}).");

        if (temperatureRecords != null)
        {
            stationsWithTempData.Add(station);
        }

        if (precipitationRecords != null)
        {
            stationsWithPrcpData.Add(station);
        }
    }
    catch (CsvFormatException ex)
    {
        logger.LogError(ex, $"File for station {station.Id} has bad data. Will skip this station.");
        stationsWithBadFiles.Add(station.Id);
    }
    catch (FileLoadException ex)
    {
        logger.LogError(ex, $"Error loading file for station {station.Id}. Will skip this station.");
        stationsWithBadFiles.Add(station.Id);
    }
});

await GhcndDataFileMappingBuilder.CreateDataFileMapping([.. stationsWithTempData.OrderBy(x => x.Id)], "temperature", "87C65C34-C689-4BA1-8061-626E4A63D401");
await GhcndDataFileMappingBuilder.CreateDataFileMapping([.. stationsWithPrcpData.OrderBy(x => x.Id)], "precipitation", "5BBEAF4C-B459-410E-9B77-470905CB1E46");

var archiveCount = stationsWithTempData.Select(x => x.Id).Union(stationsWithPrcpData.Select(x => x.Id)).Count();
logger.LogInformation(
    $"Done. {stations.Count} stations selected: wrote {archiveCount} archives to {ghcndFolder} " +
    $"({stationsWithTempData.Count} with temperature, {stationsWithPrcpData.Count} with precipitation). " +
    $"Not in GHCNd: {notInGhcnd.Count}. Not enough data: {stationsWithNoUsableData.Count}. Unreadable: {stationsWithBadFiles.Count}.");

if (!stationsWithBadFiles.IsEmpty)
{
    logger.LogWarning($"Unreadable GHCNd files: {string.Join(", ", stationsWithBadFiles.Order())}");
}

List<OutputRowTemperature>? GetTemperatureRecords(Station station, List<GhcndInputRow> rows)
{
    var temperatureRecords = GhcndTemperatureProcessor.CreateRecords(rows);
    GhcndTemperatureProcessor.ValidateRecords(temperatureRecords, station.Id, logger);

    if (!GhcndTemperatureProcessor.HasSufficientData(temperatureRecords))
    {
        logger.LogInformation($"Insufficient temperature data exists for {station.Id}. It will be excluded from the dataset.");
        return null;
    }

    return temperatureRecords;
}

List<OutputRowPrecipitation>? GetPrecipitationRecords(Station station, List<GhcndInputRow> rows)
{
    var precipitationRecords = GhcndPrecipitationProcessor.CreateRecords(rows);
    GhcndPrecipitationProcessor.ValidateRecords(precipitationRecords, station.Id, logger);

    if (!GhcndPrecipitationProcessor.HasSufficientData(precipitationRecords))
    {
        logger.LogInformation($"Insufficient precipitation data exists for {station.Id}. It will be excluded from the dataset.");
        return null;
    }

    return precipitationRecords;
}

static string Describe(List<OutputRowTemperature>? temperatureRecords, List<OutputRowPrecipitation>? precipitationRecords)
{
    return (temperatureRecords, precipitationRecords) switch
    {
        (not null, not null) => "temperature and precipitation",
        (not null, null) => "temperature",
        _ => "precipitation",
    };
}
