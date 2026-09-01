namespace ClimateExplorer.Data.Downloading.Transformers;

using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// WGMS's own glacier-list categories - see <see href="https://wgms.ch/products_ref_glaciers/"/>. WGMS's own
/// definitions additionally require at most a 1-year gap in the dataset's own most recent 10 years; this
/// transformer deliberately omits that recency requirement (see <see cref="WgmsGlacierMassBalanceSourceFileTransformer"/>).
/// </summary>
public enum WgmsGlacierFilter
{
    /// <summary>More than 10 years of ongoing records.</summary>
    Benchmark,

    /// <summary>More than 30 years of ongoing records.</summary>
    Reference,
}

/// <summary>How each year's qualifying glaciers' raw annual balances are combined into one global figure.</summary>
public enum WgmsAveragingStage
{
    /// <summary>A flat mean of every qualifying glacier's raw annual balance for the year.</summary>
    OneStage,

    /// <summary>
    /// Mean within each glacier's GTN-G region first, then mean of those region means - WGMS's own
    /// approach (see <see href="https://wgms.ch/global-glacier-state/"/>), which keeps a densely
    /// instrumented region (e.g. the Alps) from dominating the global figure. Requires
    /// <c>glacier.csv</c>'s <c>gtng_region</c> column, read from the same zip as <c>mass_balance.csv</c>.
    /// </summary>
    TwoStage,
}

/// <summary>
/// Downloads the World Glacier Monitoring Service's "Fluctuations of Glaciers" database (a zip archive)
/// and derives a single global glacier mass balance index from its <c>mass_balance.csv</c>'s raw
/// <c>annual_balance</c> values - the same quantity WGMS's own published reference-glacier regional
/// average is built from (no anomaly normalization, no per-glacier baseline). <paramref name="filter"/>
/// and <paramref name="averagingStage"/> select which of WGMS's own glacier categories contribute and how
/// their values are combined; see docs/notes/2026-08-27-wgms-reference-glacier-mass-balance-discrepancy.md for
/// the investigation and reasoning behind this design (an earlier anomaly-from-own-mean version was
/// replaced - anomalies made it harder to read what mass balance itself was doing over time, and diverged
/// from WGMS's own published figures for reasons unrelated to the glacier list).
/// </summary>
/// <param name="filter">Which WGMS glacier category qualifies a glacier to contribute.</param>
/// <param name="averagingStage">How qualifying glaciers' raw annual balances are combined per year.</param>
public sealed class WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter filter, WgmsAveragingStage averagingStage) : IDataSetSourceFileTransformer
{
    // Not part of either WGMS rule itself - suppresses years (mostly pre-1946) where only a handful of
    // glaciers have records, which would otherwise let one or two glaciers' values swing the "global"
    // index on their own.
    private const int MinimumContributingGlaciers = 5;

    public async Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken)
    {
        var records = await ReadAnnualBalanceRecordsAsync(rawFilePath, cancellationToken);
        if (records.Count == 0)
        {
            throw new InvalidDataException("WGMS mass balance source contained no usable annual balance records.");
        }

        var minimumYearsOfRecords = filter switch
        {
            WgmsGlacierFilter.Benchmark => 10,
            WgmsGlacierFilter.Reference => 30,
            _ => throw new NotSupportedException($"Unhandled {nameof(WgmsGlacierFilter)}: {filter}"),
        };

        var qualifyingGlacierIds = records
            .GroupBy(x => x.GlacierId)
            .Where(glacier => glacier.Select(x => x.Year).Distinct().Count() > minimumYearsOfRecords)
            .Select(glacier => glacier.Key)
            .ToHashSet();

        if (qualifyingGlacierIds.Count == 0)
        {
            throw new InvalidDataException($"WGMS mass balance source contained no {filter}-qualifying glaciers.");
        }

        var regionByGlacierId = averagingStage == WgmsAveragingStage.TwoStage
            ? await ReadGlacierRegionsAsync(rawFilePath, qualifyingGlacierIds, cancellationToken)
            : null;

        var valuesByYear = new SortedDictionary<int, List<(string GlacierId, double Value)>>();
        foreach (var record in records)
        {
            if (!qualifyingGlacierIds.Contains(record.GlacierId))
            {
                continue;
            }

            if (!valuesByYear.TryGetValue(record.Year, out var values))
            {
                values = [];
                valuesByYear.Add(record.Year, values);
            }

            values.Add((record.GlacierId, record.AnnualBalance));
        }

        var output = new List<string> { "Year,Value" };
        foreach (var (year, values) in valuesByYear)
        {
            if (values.Count < MinimumContributingGlaciers)
            {
                continue;
            }

            var globalMean = averagingStage == WgmsAveragingStage.TwoStage
                ? AverageByRegionThenGlobally(values, regionByGlacierId!)
                : values.Average(x => x.Value);

            output.Add($"{year},{globalMean.ToString("0.000", CultureInfo.InvariantCulture)}");
        }

        if (output.Count <= 1)
        {
            throw new InvalidDataException("WGMS mass balance source produced no years with enough contributing glaciers.");
        }

        await File.WriteAllLinesAsync(outputFilePath, output, cancellationToken);
    }

    private static double AverageByRegionThenGlobally(List<(string GlacierId, double Value)> values, IReadOnlyDictionary<string, string> regionByGlacierId)
    {
        return values
            .GroupBy(x => regionByGlacierId[x.GlacierId])
            .Select(region => region.Average(x => x.Value))
            .Average();
    }

    private static async Task<List<AnnualBalanceRecord>> ReadAnnualBalanceRecordsAsync(string rawFilePath, CancellationToken cancellationToken)
    {
        using var zipFileStream = new FileStream(rawFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault(x => x.FullName.Replace('\\', '/').EndsWith("mass_balance.csv", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("WGMS archive did not contain a mass_balance.csv entry.");

        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
        };
        using var csv = new CsvReader(reader, csvConfiguration);

        var records = new List<AnnualBalanceRecord>();
        await foreach (var row in csv.GetRecordsAsync<MassBalanceRow>(cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(row.GlacierId) ||
                !int.TryParse(row.Year, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) ||
                string.IsNullOrWhiteSpace(row.AnnualBalance) ||
                !double.TryParse(row.AnnualBalance, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                !double.IsFinite(value))
            {
                continue;
            }

            records.Add(new AnnualBalanceRecord(row.GlacierId, year, value));
        }

        return records;
    }

    /// <summary>Reads glacier_id -&gt; gtng_region for every id in <paramref name="glacierIds"/> from the zip's glacier.csv.</summary>
    private static async Task<Dictionary<string, string>> ReadGlacierRegionsAsync(string rawFilePath, HashSet<string> glacierIds, CancellationToken cancellationToken)
    {
        using var zipFileStream = new FileStream(rawFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault(x => x.FullName.Replace('\\', '/').EndsWith("glacier.csv", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("WGMS archive did not contain a glacier.csv entry.");

        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
        };
        using var csv = new CsvReader(reader, csvConfiguration);

        var regionByGlacierId = new Dictionary<string, string>();
        await foreach (var row in csv.GetRecordsAsync<GlacierRegionRow>(cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(row.GlacierId) ||
                string.IsNullOrWhiteSpace(row.Region) ||
                !glacierIds.Contains(row.GlacierId) ||
                regionByGlacierId.ContainsKey(row.GlacierId))
            {
                continue;
            }

            regionByGlacierId[row.GlacierId] = row.Region;
        }

        var missingRegion = glacierIds.Except(regionByGlacierId.Keys).FirstOrDefault();
        if (missingRegion != null)
        {
            throw new InvalidDataException($"WGMS archive's glacier.csv did not contain a gtng_region for glacier_id '{missingRegion}'.");
        }

        return regionByGlacierId;
    }

    private sealed record AnnualBalanceRecord(string GlacierId, int Year, double AnnualBalance);

    private sealed class MassBalanceRow
    {
        [Name("glacier_id")]
        public string? GlacierId { get; set; }

        [Name("year")]
        public string? Year { get; set; }

        [Name("annual_balance")]
        public string? AnnualBalance { get; set; }
    }

    private sealed class GlacierRegionRow
    {
        [Name("id")]
        public string? GlacierId { get; set; }

        [Name("gtng_region")]
        public string? Region { get; set; }
    }
}
