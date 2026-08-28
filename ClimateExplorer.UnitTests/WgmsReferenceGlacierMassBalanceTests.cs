namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Data.Downloading.Transformers;
using CsvHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Runs the actual, unmodified <see cref="WgmsGlacierMassBalanceSourceFileTransformer"/> against real
/// WGMS "Fluctuations of Glaciers" 2026-02-10 data to confirm it closely reproduces WGMS's own published
/// reference-glacier regional average (<c>GlacierFixtures/mb_ref.csv</c>, from
/// <see href="https://wgms.ch/global-glacier-state/"/>) when configured the way WGMS computes it
/// (Reference filter, two-stage regional averaging), and to quantify how much using the wider Benchmark
/// filter instead - what the shipped "Global glacier mass balance" preset actually uses - changes the
/// result. See docs/notes/2026-08-27-wgms-reference-glacier-mass-balance-discrepancy.md.
///
/// All fixture data in <c>GlacierFixtures/</c> is real data trimmed from WGMS's "Fluctuations of
/// Glaciers" 2026-02-10 release (<c>DOI-WGMS-FoG-2026-02-10.zip</c>, <see href="https://wgms.ch/downloads/"/>),
/// not synthetic - these tests exist to pin down real-world behaviour, not just exercise code paths.
/// </summary>
[TestClass]
public sealed class WgmsReferenceGlacierMassBalanceTests
{
    // WGMS's own "Reference Glacier" network as published at https://wgms.ch/products_ref_glaciers/
    // (61 glaciers, "more than 30 years of ongoing glaciological mass-balance measurements"), spelled as
    // in mass_balance.csv's glacier_name column. Captured 2026-08-27.
    private static readonly HashSet<string> OfficialReferenceGlacierNames = new(StringComparer.Ordinal)
    {
        "GULKANA", "WOLVERINE",
        "COLUMBIA (2057)", "EASTON", "LEMON CREEK", "RAINBOW", "SOUTH CASCADE", "HELM", "PEYTO", "PLACE",
        "DEVON ICE CAP NW", "MEIGHEN ICE CAP", "MELVILLE SOUTH ICE CAP", "WHITE",
        "BRUARJOKULL", "EYJABAKKAJOKULL", "HOFSJOEKULL E", "HOFSJOEKULL N", "HOFSJOEKULL SW", "TUNGNAARJOKULL",
        "AUSTRE BROEGGERBREEN", "MIDTRE LOVENBREEN",
        "AALFOTBREEN", "ENGABREEN", "GRAASUBREEN", "HELLSTUGUBREEN", "LANGFJORDJOEKELEN", "NIGARDSBREEN",
        "REMBESDALSKAAKA", "STORBREEN", "MARMAGLACIAEREN", "RABOTS GLACIAER", "RIUKOJIETNA", "STORGLACIAEREN",
        "GOLDBERG K.", "HINTEREIS F.", "JAMTAL F.", "KESSELWAND F.", "PASTERZE", "VERNAGT F.",
        "ALLALIN", "BASODINO", "CLARIDENFIRN", "GIETRO", "GRIES", "SILVRETTA",
        "MALADETA", "ARGENTIERE", "SAINT SORLIN", "CARESER", "CIARDONEY",
        "DJANKUAT", "GARABASHI", "LEVIY AKTRU",
        "ABRAMOV", "GOLUBIN", "KARA-BATKAK", "TS.TUYUKSUYSKIY", "URUMQI GLACIER NO. 1",
        "ZONGO", "ECHAURREN NORTE",
    };

    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerWgmsRefTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(temporaryRoot))
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    /// <summary>
    /// Background, not a proposal to change the transformer's own (deliberately looser) Benchmark filter:
    /// does the transformer's mechanical "more than 30 years, at most 1 year's gap in the dataset's own
    /// most recent decade" rule reproduce WGMS's own curated reference-glacier list? It almost does, which
    /// is why the next test's "close but not exact" result is expected rather than a red flag.
    /// </summary>
    [TestMethod]
    public void ReferenceFilterYearsAndGapRule_AppliedToRealWgmsData_AlmostReproducesOfficialReferenceGlacierList()
    {
        var byGlacierYears = new Dictionary<string, (string Name, HashSet<int> Years)>();
        foreach (var (glacierId, name, year, _) in ReadRealMassBalanceRows())
        {
            if (!byGlacierYears.TryGetValue(glacierId, out var glacier))
            {
                byGlacierYears[glacierId] = glacier = (name, []);
            }

            glacier.Years.Add(year);
        }

        var maxYear = byGlacierYears.Values.SelectMany(g => g.Years).Max();
        var decadeStart = maxYear - 9;

        var qualifyingNames = byGlacierYears.Values
            .Where(g => QualifiesUnderYearsAndGapRule(g.Years, minYears: 30, decadeStart, maxYear, maxGap: 1))
            .Select(g => g.Name)
            .ToHashSet();

        // The rule finds every official reference glacier except LEVIY AKTRU, which has a 2013-2018
        // reporting gap that fails "at most one year's gap in the most recent decade" even though it has
        // resumed reporting every year since 2019 and has 43 years of records overall.
        var missingOfficial = OfficialReferenceGlacierNames.Except(qualifyingNames).ToList();
        CollectionAssert.AreEquivalent(new[] { "LEVIY AKTRU" }, missingOfficial);

        // ...and it also finds 22 extra glaciers that satisfy the numeric rule but aren't on WGMS's
        // curated list (e.g. TAKU, RHONE, KONGSVEGEN) - WGMS's selection includes additional criteria
        // ("primarily climate-driven... no major avalanche/calving/surge influence") a years-and-gap rule
        // can't see. WGMS's list is curated/maintained, not mechanically re-derived every release.
        var extraQualifying = qualifyingNames.Except(OfficialReferenceGlacierNames).ToList();
        Assert.HasCount(22, extraQualifying);
        Assert.HasCount(82, qualifyingNames);
    }

    [TestMethod]
    public async Task TransformAsync_ReferenceFilterTwoStage_OnRealData_CloselyReproducesWgmsPublishedIndex()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateFullWgmsZipFixture(input);

        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Reference, WgmsAveragingStage.TwoStage)
            .TransformAsync(input, output, CancellationToken.None);

        var actual = ParseYearValueCsv(output);
        var wgmsRegionAverage = LoadMbRefRegionAverageInMetres();
        var commonYears = actual.Keys.Intersect(wgmsRegionAverage.Keys).ToList();

        // Sanity check: broad overlap with mb_ref.csv's year range, not just a lucky handful of years.
        Assert.IsGreaterThan(60, commonYears.Count);

        // The transformer's own mechanical Reference filter doesn't exactly reproduce WGMS's 61-glacier
        // curated list (previous test: 1 false negative, 22 false positives) - but using WGMS's own raw
        // value / two-stage-regional method, the result is still close: comfortably under 0.2 m w.e. mean
        // absolute difference across the record, and no single year is wildly off (worst observed, 1985,
        // is ~0.51 m w.e. - most of that gap is attributable to the mismatched glacier list, not the
        // calculation method, per the previous test).
        var diffs = commonYears.Select(year => Math.Abs(actual[year] - wgmsRegionAverage[year])).ToList();
        var meanAbsDiff = diffs.Average();
        Assert.IsLessThan(0.2, meanAbsDiff, $"mean abs diff across {diffs.Count} years was {meanAbsDiff:0.000} m w.e.");

        foreach (var year in commonYears)
        {
            var diff = Math.Abs(actual[year] - wgmsRegionAverage[year]);
            Assert.IsLessThan(0.6, diff, $"year {year}: transformer={actual[year]:0.000}, wgms={wgmsRegionAverage[year]:0.000}");
        }
    }

    [TestMethod]
    public async Task TransformAsync_BenchmarkVsReferenceFilter_BothTwoStage_OnRealData_ProduceSimilarValues()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        CreateFullWgmsZipFixture(input);

        var benchmarkOutput = Path.Combine(temporaryRoot, "benchmark-output.csv");
        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.TwoStage)
            .TransformAsync(input, benchmarkOutput, CancellationToken.None);

        var referenceOutput = Path.Combine(temporaryRoot, "reference-output.csv");
        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Reference, WgmsAveragingStage.TwoStage)
            .TransformAsync(input, referenceOutput, CancellationToken.None);

        var benchmark = ParseYearValueCsv(benchmarkOutput);
        var reference = ParseYearValueCsv(referenceOutput);
        var commonYears = benchmark.Keys.Intersect(reference.Keys).ToList();

        // Both filters draw from the same dataset-wide year range in practice, so they should cover
        // (almost) exactly the same years.
        Assert.IsGreaterThan(benchmark.Count - 3, commonYears.Count);

        // The wider (138-glacier) Benchmark filter and the stricter (82-glacier, mechanically) Reference
        // filter track each other closely once both use two-stage regional averaging - confirming that,
        // for the "Global glacier mass balance" preset's choice of Benchmark+TwoStage, the glacier-list
        // choice matters far less than the raw-value/two-stage-averaging choice did (that changed the
        // result by ~0.53 m w.e. on average - see the notes doc's comparison against the old
        // anomaly/flat-average method).
        var diffs = commonYears.Select(year => Math.Abs(benchmark[year] - reference[year])).ToList();
        var meanAbsDiff = diffs.Average();
        Assert.IsLessThan(0.1, meanAbsDiff, $"mean abs diff across {diffs.Count} years was {meanAbsDiff:0.000} m w.e.");
    }

    private static bool QualifiesUnderYearsAndGapRule(HashSet<int> years, int minYears, int decadeStart, int decadeEnd, int maxGap)
    {
        if (years.Count <= minYears)
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

        return missingYears <= maxGap;
    }

    private static Dictionary<int, double> ParseYearValueCsv(string path)
    {
        var result = new Dictionary<int, double>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var parts = line.Split(',');
            result[int.Parse(parts[0], CultureInfo.InvariantCulture)] = double.Parse(parts[1], CultureInfo.InvariantCulture);
        }

        return result;
    }

    private static Dictionary<int, double> LoadMbRefRegionAverageInMetres()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GlacierFixtures", "mb_ref.csv");
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();

        var result = new Dictionary<int, double>();
        while (csv.Read())
        {
            var year = csv.GetField<int>("Year");
            var regionAverageMm = csv.GetField<double>("REF_regionAVG");
            result[year] = regionAverageMm / 1000.0;
        }

        return result;
    }

    /// <summary>Reads GlacierFixtures/mass_balance_all_glaciers.csv directly (not via a zip) for the background test above.</summary>
    private static IEnumerable<(string GlacierId, string Name, int Year, double AnnualBalance)> ReadRealMassBalanceRows()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GlacierFixtures", "mass_balance_all_glaciers.csv");
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var glacierId = csv.GetField("glacier_id");
            var annualBalance = csv.GetField("annual_balance");
            if (string.IsNullOrWhiteSpace(glacierId) || string.IsNullOrWhiteSpace(annualBalance))
            {
                continue;
            }

            yield return (glacierId, csv.GetField("glacier_name")!, csv.GetField<int>("year"), double.Parse(annualBalance, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Packages the real GlacierFixtures CSVs into a zip shaped like a genuine WGMS release, for TransformAsync to read.</summary>
    private static void CreateFullWgmsZipFixture(string zipPath)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(Path.Combine(AppContext.BaseDirectory, "GlacierFixtures", "mass_balance_all_glaciers.csv"), "data/mass_balance.csv");
        archive.CreateEntryFromFile(Path.Combine(AppContext.BaseDirectory, "GlacierFixtures", "glacier_regions.csv"), "data/glacier.csv");
    }
}
