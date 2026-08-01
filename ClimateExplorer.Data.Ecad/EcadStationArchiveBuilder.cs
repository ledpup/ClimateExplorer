namespace ClimateExplorer.Data.Ecad;

using System.IO.Compression;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Writes a station's published series into the per-station zip the runtime reads, matching how BOM and
/// GHCNd store their daily stations. Compression matters here: a European station's full history runs to
/// well over a megabyte of CSV, and these files are checked in.
/// </summary>
public static class EcadStationArchiveBuilder
{
    public static string GetArchiveEntryPath(string ghcnStationId) => $"{ghcnStationId}.csv";

    public static async Task BuildAsync(
        IEnumerable<EcadDailyObservation> observations,
        string ghcnStationId,
        string archivePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ghcnStationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        var rows = observations.Where(x => x.HasAnyValue).OrderBy(x => x.Date).ToList();
        if (rows.Count == 0)
        {
            throw new InvalidDataException($"ECA&D returned no observations for station '{ghcnStationId}'.");
        }

        var incomplete = EcadConstants.PublishedDataTypes.Where(x => !HasAnyValue(rows, x)).ToList();
        if (incomplete.Count > 0)
        {
            // All four measurements read the same asset, so download validation fails the whole station if
            // any one column is empty. Failing here says which column, rather than leaving the runtime to
            // report that the file "contained no finite measurements".
            throw new InvalidDataException(
                $"ECA&D returned no {string.Join(", ", incomplete)} observations for station '{ghcnStationId}'.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        await using var archiveStream = new FileStream(archivePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: false);
        var entry = archive.CreateEntry(GetArchiveEntryPath(ghcnStationId), CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream);
        await writer.WriteAsync(EcadCsvFormat.Write(rows).AsMemory(), cancellationToken);
    }

    public static IReadOnlyList<string> ReadArchive(string archivePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.Entries.SingleOrDefault()
            ?? throw new InvalidDataException($"ECA&D archive '{archivePath}' does not contain exactly one entry.");

        using var reader = new StreamReader(entry.Open());
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }

    private static bool HasAnyValue(IEnumerable<EcadDailyObservation> rows, DataType dataType)
    {
        return rows.Any(x => x[dataType].HasValue);
    }
}
