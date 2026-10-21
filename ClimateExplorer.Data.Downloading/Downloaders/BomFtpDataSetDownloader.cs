namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Downloading.Workspace;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Brings a published BOM station archive up to date from BOM's daily station tables on its anonymous FTP server,
/// appending only the days after each entry's last record, in the same row format as the Climate Data Online files
/// the archive was built from. The full history still comes from CDO via the batch tools. See
/// docs/design/2026-10-21-03-bom-ftp-daily-refresh-plan.md.
/// </summary>
public sealed class BomFtpDataSetDownloader(
    IBomDailyTableSource tableSource,
    DataSetSourceFileStore sourceFileStore,
    TimeProvider timeProvider,
    string? stationMetadataFolder = null) : IDataSetDownloader
{
    private const string TablesPath = "/anon/gen/clim_data/IDCKWCDEA0/tables";

    // The second line of each table's column header, checked so a change to the table layout fails the download
    // instead of reading the wrong columns.
    private const string ExpectedColumns = ",,Transpiration,Rain,Evaporation,Maximum,Minimum,Relative,Relative,10m Wind,Solar";

    // A gap longer than this is left for the batch tools to fill.
    private const int MaximumMonths = 24;

    private readonly string stationMetadataFolder = stationMetadataFolder ?? Path.Combine("MetaData", "Station");

    public string Key => "bom-station";

    /// <summary>
    /// The table folder for a station: its state, from the BOM station number, and its name in lower case with
    /// spaces and slashes as underscores and full stops removed (e.g. 066214 "SYDNEY (OBSERVATORY HILL)" is
    /// nsw/sydney_(observatory_hill)). Station names in the metadata are kept in line with BOM's folder names.
    /// </summary>
    public static string GetTableFolder(string stationId, string stationName)
    {
        var state = int.Parse(stationId[..3], CultureInfo.InvariantCulture) switch
        {
            <= 13 => "wa",
            <= 15 => "nt",
            <= 26 => "sa",
            <= 45 => "qld",
            <= 75 => "nsw",
            <= 90 => "vic",
            _ => "tas",
        };

        var name = stationName.ToLowerInvariant().Replace(' ', '_').Replace('/', '_').Replace(".", string.Empty, StringComparison.Ordinal);
        return $"{state}/{name}";
    }

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
        var folder = GetTableFolder(stationId, await GetStationNameAsync(request.DataSetDefinition, stationId));
        var entries = ReadEntries(sourceFileStore.ResolvePath(request.RelativePath));

        var measurements = request.Measurements.Select(x => x.MeasurementDefinition).ToList();
        var lastDates = measurements.ToDictionary(
            x => x.DataType,
            x => GetLastDate(x, entries.GetValueOrDefault(GetEntryPath(x, stationId)) ?? []));
        var rows = await DownloadRowsAsync(folder, lastDates.Values.Min(), cancellationToken);

        foreach (var measurement in measurements)
        {
            var entryPath = GetEntryPath(measurement, stationId);
            var lastDate = lastDates[measurement.DataType];
            var newLines = rows
                .Where(x => lastDate is null || x.Date > lastDate)
                .Select(x => FormatRow(measurement.DataType, stationId, x))
                .OfType<string>();
            entries[entryPath] = [.. entries.GetValueOrDefault(entryPath) ?? [], .. newLines];
        }

        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(candidatePath)!);
        await using var archiveStream = new FileStream(candidatePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: false);
        foreach (var (entryPath, lines) in entries)
        {
            await using var entryStream = archive.CreateEntry(entryPath, CompressionLevel.Optimal).Open();
            await using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync(string.Join('\n', lines).AsMemory(), cancellationToken);
            await writer.WriteAsync('\n');
        }

        return new DataSetDownloadArtifact(candidatePath);
    }

    private static string GetEntryPath(MeasurementDefinition measurement, string stationId)
    {
        var entryPathFormat = measurement.DataFileSource.ArchiveEntryPathFormat
            ?? throw new InvalidOperationException("Every BOM measurement must resolve to an archive entry.");
        return entryPathFormat.Replace("[station]", stationId, StringComparison.Ordinal).Replace('\\', '/');
    }

    private static Dictionary<string, List<string>> ReadEntries(string archivePath)
    {
        // There is nothing to top up without the archive's history, which only the batch tools can build.
        using var archive = ZipFile.OpenRead(archivePath);
        var entries = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            using var reader = new StreamReader(entry.Open());
            entries[entry.FullName] = reader.ReadToEnd()
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Where(x => x.Length > 0)
                .ToList();
        }

        return entries;
    }

    private static DateOnly? GetLastDate(MeasurementDefinition measurement, IReadOnlyList<string> lines)
    {
        var regex = new Regex(measurement.DataRowRegEx!);
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var match = regex.Match(lines[i]);
            if (match.Success)
            {
                return new DateOnly(
                    int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture));
            }
        }

        return null;
    }

    private static string? FormatRow(DataType dataType, string stationId, DailyRow row)
    {
        var date = string.Create(CultureInfo.InvariantCulture, $"{stationId},{row.Date:yyyy},{row.Date:MM},{row.Date:dd}");
        return dataType switch
        {
            DataType.TempMax => row.Maximum is { } maximum ? $"IDCJAC0010,{date},{Format(maximum)},1,N" : null,
            DataType.TempMin => row.Minimum is { } minimum ? $"IDCJAC0011,{date},{Format(minimum)},1,N" : null,
            DataType.Precipitation => row.Rainfall is { } rainfall ? $"IDCJAC0009,{date},{Format(rainfall)},1,N" : null,
            DataType.SolarRadiation => row.SolarRadiation is { } solar ? $"IDCJAC0016,{date},{Format(Math.Round(solar, 1))}" : null,
            DataType.TempMean => row.Maximum is { } maximum && row.Minimum is { } minimum
                ? string.Create(CultureInfo.InvariantCulture, $"{row.Date:yyyyMMdd},{Math.Round((maximum + minimum) / 2D, 2):0.##}")
                : null,
            _ => throw new NotSupportedException($"No BOM daily table column is configured for {dataType}."),
        };
    }

    private static string Format(double value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private async Task<string> GetStationNameAsync(DataSetDefinition dataSetDefinition, string stationId)
    {
        var stations = await Station.GetStationsFromFile(Path.Combine(stationMetadataFolder, dataSetDefinition.StationMetadataFileName!));
        return stations.SingleOrDefault(x => x.Id == stationId)?.Name
            ?? throw new InvalidOperationException($"No station name is recorded for BOM station '{stationId}'.");
    }

    private async Task<IReadOnlyList<DailyRow>> DownloadRowsAsync(string folder, DateOnly? lastDate, CancellationToken cancellationToken)
    {
        // Australian dates run up to a day ahead of UTC.
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(1);
        var lastMonth = new DateOnly(today.Year, today.Month, 1);
        var firstDay = lastDate?.AddDays(1) ?? today;
        var firstMonth = new DateOnly(firstDay.Year, firstDay.Month, 1);
        if (firstMonth < lastMonth.AddMonths(1 - MaximumMonths))
        {
            firstMonth = lastMonth.AddMonths(1 - MaximumMonths);
        }

        var name = folder[(folder.IndexOf('/') + 1)..];
        var rows = new List<DailyRow>();
        var tableFound = false;
        for (var month = firstMonth; month <= lastMonth; month = month.AddMonths(1))
        {
            var content = await tableSource.DownloadAsync($"{TablesPath}/{folder}/{name}-{month:yyyyMM}.csv", cancellationToken);
            if (content == null)
            {
                continue;
            }

            if (!content.Contains(ExpectedColumns, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"BOM daily table '{folder}' for {month:yyyy-MM} doesn't have the expected columns.");
            }

            tableFound = true;
            rows.AddRange(ParseRows(content));
        }

        if (!tableFound)
        {
            throw new InvalidDataException($"BOM publishes no daily table at '{folder}' for {firstMonth:yyyy-MM} to {lastMonth:yyyy-MM}.");
        }

        return rows;
    }

    private static IEnumerable<DailyRow> ParseRows(string content)
    {
        foreach (var line in content.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            // Station Name, Date, Evapotranspiration, Rain, Pan Evaporation, Maximum Temperature,
            // Minimum Temperature, Maximum and Minimum Relative Humidity, 10m Wind Speed, Solar Radiation
            var fields = line.Split(',');
            if (fields.Length < 11 ||
                !DateOnly.TryParseExact(fields[1], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                continue;
            }

            yield return new DailyRow(date, Parse(fields[3]), Parse(fields[5]), Parse(fields[6]), Parse(fields[10]));
        }
    }

    private static double? Parse(string value)
    {
        return double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private sealed record DailyRow(DateOnly Date, double? Rainfall, double? Maximum, double? Minimum, double? SolarRadiation);
}
