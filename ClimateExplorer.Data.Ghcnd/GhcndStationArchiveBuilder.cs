namespace ClimateExplorer.Data.Ghcnd;

using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using Microsoft.Extensions.Logging.Abstractions;

public static class GhcndStationArchiveBuilder
{
    public static async Task BuildAsync(
        string csvContent,
        string stationId,
        string archivePath,
        bool includeTemperature,
        bool includePrecipitation,
        CancellationToken cancellationToken)
    {
        if (!includeTemperature && !includePrecipitation)
        {
            throw new InvalidOperationException("A GHCNd station archive must contain at least one measurement family.");
        }

        var rows = GhcndCsvReader.RemoveRowsWithNoData(GhcndCsvReader.ReadRows(csvContent));
        if (rows.Count == 0)
        {
            throw new InvalidDataException($"GHCNd returned no observations for station '{stationId}'.");
        }

        List<OutputRowTemperature>? temperatureRecords = null;
        if (includeTemperature)
        {
            temperatureRecords = GhcndTemperatureProcessor.CreateRecords(rows);
            GhcndTemperatureProcessor.ValidateRecords(temperatureRecords, stationId, NullLogger.Instance);
            temperatureRecords = GhcndTemperatureProcessor.FilterSufficientData(temperatureRecords);
            if (temperatureRecords.Count < 10)
            {
                throw new InvalidDataException($"GHCNd returned insufficient temperature observations for station '{stationId}'.");
            }
        }

        List<OutputRowPrecipitation>? precipitationRecords = null;
        if (includePrecipitation)
        {
            precipitationRecords = GhcndPrecipitationProcessor.CreateRecords(rows);
            GhcndPrecipitationProcessor.ValidateRecords(precipitationRecords, stationId, NullLogger.Instance);
            precipitationRecords = GhcndPrecipitationProcessor.FilterSufficientData(precipitationRecords);
            if (precipitationRecords.Count < 10)
            {
                throw new InvalidDataException($"GHCNd returned insufficient precipitation observations for station '{stationId}'.");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        await using var archiveStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: false);
        if (temperatureRecords != null)
        {
            await WriteEntryAsync(archive, $"Temperature/{stationId}.csv", temperatureRecords, cancellationToken);
        }

        if (precipitationRecords != null)
        {
            await WriteEntryAsync(archive, $"Precipitation/{stationId}.csv", precipitationRecords, cancellationToken);
        }
    }

    private static async Task WriteEntryAsync<T>(
        ZipArchive archive,
        string entryName,
        IEnumerable<T> records,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(records, cancellationToken);
    }
}
