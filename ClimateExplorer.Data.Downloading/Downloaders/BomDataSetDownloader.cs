namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Workspace;
using static ClimateExplorer.Core.Enums;

public sealed class BomDataSetDownloader(BomDailyDataClient client, TimeSpan requestPacing = default) : IDataSetDownloader
{
    private readonly BomDailyDataClient client = client;
    private readonly TimeSpan requestPacing = requestPacing;

    public string Key => "bom-station";

    public async Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        var stationIds = request.Measurements.Select(x => x.FileFilter.Id).Distinct(StringComparer.Ordinal).ToList();
        if (stationIds.Count != 1)
        {
            throw new InvalidOperationException("A BOM source asset must resolve to exactly one station.");
        }

        var stationId = stationIds[0];

        // Downloaded sequentially, not with Task.WhenAll, so a station refresh (4 series, 2 BOM requests
        // each) doesn't fire 8 near-simultaneous requests at BOM - a burst pattern that looks like the kind
        // of thing anti-bot/rate-limiting protection is built to catch, even from an otherwise low-volume caller.
        var dataTypes = new[] { DataType.TempMax, DataType.TempMin, DataType.Precipitation, DataType.SolarRadiation };
        var contents = new Dictionary<DataType, string>();
        foreach (var dataType in dataTypes)
        {
            if (contents.Count > 0 && requestPacing > TimeSpan.Zero)
            {
                await Task.Delay(requestPacing, cancellationToken);
            }

            contents[dataType] = await client.DownloadCsvAsync(stationId, GetObservationCode(dataType), cancellationToken);
        }
        contents.Add(DataType.TempMean, CreateMeanTemperature(request, stationId, contents[DataType.TempMax], contents[DataType.TempMin]));
        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(candidatePath)!);
        await using var archiveStream = new FileStream(candidatePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: false);

        foreach (var measurement in request.Measurements)
        {
            var entryPathFormat = measurement.MeasurementDefinition.DataFileSource.ArchiveEntryPathFormat
                ?? throw new InvalidOperationException("Every BOM measurement must resolve to an archive entry.");
            var entryPath = entryPathFormat.Replace("[station]", stationId, StringComparison.Ordinal).Replace('\\', '/');
            var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync(contents[measurement.MeasurementDefinition.DataType].AsMemory(), cancellationToken);
        }

        return new DataSetDownloadArtifact(candidatePath);
    }

    private static BomDailyObservationCode GetObservationCode(DataType dataType)
    {
        return dataType switch
        {
            DataType.TempMax => BomDailyObservationCode.TemperatureMaximum,
            DataType.TempMin => BomDailyObservationCode.TemperatureMinimum,
            DataType.Precipitation => BomDailyObservationCode.Rainfall,
            DataType.SolarRadiation => BomDailyObservationCode.SolarRadiation,
            _ => throw new NotSupportedException($"No BOM daily observation code is configured for {dataType}."),
        };
    }

    private static string CreateMeanTemperature(
        DataSetDownloadRequest request,
        string stationId,
        string maximumContent,
        string minimumContent)
    {
        var maximumDefinition = request.Measurements.Single(x => x.MeasurementDefinition.DataType == DataType.TempMax).MeasurementDefinition;
        var minimumDefinition = request.Measurements.Single(x => x.MeasurementDefinition.DataType == DataType.TempMin).MeasurementDefinition;
        var maximumRecords = ProcessTemperature(maximumDefinition, maximumContent, stationId)
            .Where(x => x.Value.HasValue)
            .ToDictionary(x => x.Date!.Value, x => x);
        var minimumRecords = ProcessTemperature(minimumDefinition, minimumContent, stationId)
            .Where(x => x.Value.HasValue)
            .ToDictionary(x => x.Date!.Value, x => x);

        var output = maximumRecords.Values
            .Where(x => minimumRecords.ContainsKey(x.Date!.Value))
            .OrderBy(x => x.Date)
            .Select(x =>
            {
                var value = Math.Round((x.Value!.Value + minimumRecords[x.Date!.Value].Value!.Value) / 2D, 2);
                return $"{x.Date:yyyyMMdd},{value.ToString("0.##", CultureInfo.InvariantCulture)}";
            })
            .ToList();
        if (output.Count == 0)
        {
            throw new InvalidDataException($"BOM maximum and minimum temperature did not overlap for station '{stationId}'.");
        }

        return string.Join(Environment.NewLine, output) + Environment.NewLine;
    }

    private static IEnumerable<DataRecord> ProcessTemperature(
        MeasurementDefinition definition,
        string content,
        string stationId)
    {
        return DataReaderFunctions.ProcessDataFile(
            content.Split(["\r\n", "\n"], StringSplitOptions.None),
            new Regex(definition.DataRowRegEx!),
            definition.NullValue!,
            definition.DataResolution,
            stationId).Values;
    }
}
