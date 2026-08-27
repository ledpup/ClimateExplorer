namespace ClimateExplorer.Data.Downloading.Transformers;

using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// Downloads the World Glacier Monitoring Service's "Fluctuations of Glaciers" database (a zip archive)
/// and derives a single global glacier mass balance index from its <c>mass_balance.csv</c>.
///
/// Only "Benchmark" glaciers (WGMS's own definition: more than 10 years of ongoing measurements, with at
/// most a 1-year gap across the most recent 10 years of the dataset) contribute. Each contributing
/// glacier's annual balance is expressed as a deviation from its own all-time mean before the per-year
/// average is taken, so the index isn't biased toward whichever glaciers happen to have data in a given
/// year, and glaciers with longer or more complete records don't dominate the signal.
/// </summary>
public sealed class WgmsGlacierMassBalanceSourceFileTransformer : IDataSetSourceFileTransformer
{
    // WGMS's "Benchmark" glacier rule: "more than 10 years of ongoing glaciological mass-balance
    // measurements... maximum observational gap of one year in the past decade".
    private const int MinimumYearsOfRecords = 10;
    private const int RecentDecadeWindowYears = 10;
    private const int MaximumRecentGapYears = 1;

    // Not part of the Benchmark rule itself - suppresses years (mostly pre-1946) where only a
    // handful of glaciers have records, which would otherwise let one or two glaciers' anomalies
    // swing the "global" index on their own.
    private const int MinimumContributingGlaciers = 5;

    public async Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken)
    {
        var records = await ReadAnnualBalanceRecordsAsync(rawFilePath, cancellationToken);
        if (records.Count == 0)
        {
            throw new InvalidDataException("WGMS mass balance source contained no usable annual balance records.");
        }

        var maxYear = records.Max(x => x.Year);
        var decadeStart = maxYear - RecentDecadeWindowYears + 1;

        var anomaliesByYear = new SortedDictionary<int, List<double>>();
        var qualifyingGlacierCount = 0;
        foreach (var glacier in records.GroupBy(x => x.GlacierId))
        {
            var years = glacier.Select(x => x.Year).ToHashSet();
            if (!IsBenchmarkGlacier(years, decadeStart, maxYear))
            {
                continue;
            }

            qualifyingGlacierCount++;
            var meanBalance = glacier.Average(x => x.AnnualBalance);
            foreach (var record in glacier)
            {
                if (!anomaliesByYear.TryGetValue(record.Year, out var values))
                {
                    values = [];
                    anomaliesByYear.Add(record.Year, values);
                }

                values.Add(record.AnnualBalance - meanBalance);
            }
        }

        if (qualifyingGlacierCount == 0)
        {
            throw new InvalidDataException("WGMS mass balance source contained no Benchmark-qualifying glaciers.");
        }

        var output = new List<string> { "Year,Value" };
        output.AddRange(
            anomaliesByYear
                .Where(x => x.Value.Count >= MinimumContributingGlaciers)
                .Select(x => $"{x.Key},{x.Value.Average().ToString("0.000", CultureInfo.InvariantCulture)}"));

        if (output.Count <= 1)
        {
            throw new InvalidDataException("WGMS mass balance source produced no years with enough contributing glaciers.");
        }

        await File.WriteAllLinesAsync(outputFilePath, output, cancellationToken);
    }

    private static bool IsBenchmarkGlacier(HashSet<int> years, int decadeStart, int decadeEnd)
    {
        if (years.Count <= MinimumYearsOfRecords)
        {
            return false;
        }

        var missingYears = 0;
        for (var year = decadeStart; year <= decadeEnd; year++)
        {
            if (!years.Contains(year))
            {
                missingYears++;
            }
        }

        return missingYears <= MaximumRecentGapYears;
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
}
