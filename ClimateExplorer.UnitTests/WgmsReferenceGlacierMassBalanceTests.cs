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
using CsvHelper.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Quantifies why <see cref="WgmsGlacierMassBalanceSourceFileTransformer"/>'s output and WGMS's own
/// published reference-glacier regional average (<c>GlacierFixtures/mb_ref.csv</c>, captured from
/// <see href="https://wgms.ch/global-glacier-state/"/>) carry different numbers. They're two different
/// metrics by design (a different glacier list, and a different calculation method) - this suite doesn't
/// argue either is more correct, it pins down how much each difference contributes.
/// See notes/2026-08-27-wgms-reference-glacier-mass-balance-discrepancy.md for the write-up this backs.
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

    // Representative years spanning the record (thin early years, the well-sampled late-20th-century
    // decades, and the extreme 2022/2023 melt years) used to spot-check divergence/agreement without
    // asserting every one of ~75 years by hand.
    private static readonly int[] SampleYears = [1955, 1965, 1985, 1998, 2003, 2022, 2023, 2025];

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
    /// Background, not a proposal to change the transformer's existing (deliberate) 10-year Benchmark
    /// rule: does a mechanical "more than 30 years, at most 1 year's gap in the dataset's own most recent
    /// decade" rule (the same shape, just with WGMS's quoted reference-glacier threshold) reproduce WGMS's
    /// own curated reference-glacier list?
    /// </summary>
    [TestMethod]
    public void MoreThan30YearRule_AppliedToRealWgmsData_AlmostReproducesOfficialReferenceGlacierList()
    {
        var byGlacierId = LoadRealAnnualBalances();
        var maxYear = byGlacierId.Values.SelectMany(g => g.Years.Keys).Max();
        var decadeStart = maxYear - 9;

        var qualifyingNames = byGlacierId.Values
            .Where(g => QualifiesUnderYearsAndGapRule(g.Years.Keys.ToHashSet(), 30, decadeStart, maxYear, maxGap: 1))
            .Select(g => g.Name)
            .ToHashSet();

        // The rule finds every official reference glacier except LEVIY AKTRU, which has a 2013-2018
        // reporting gap that fails "at most one year's gap in the most recent decade" even though it has
        // resumed reporting every year since 2019 and has 43 years of records overall - a real gap
        // between "more than 30 years of data" and WGMS's own maintained/curated reference-glacier list.
        var missingOfficial = OfficialReferenceGlacierNames.Except(qualifyingNames).ToList();
        CollectionAssert.AreEquivalent(new[] { "LEVIY AKTRU" }, missingOfficial);

        // ...and it also finds 22 extra glaciers that satisfy the numeric rule but aren't on WGMS's
        // curated list (e.g. TAKU, RHONE, KONGSVEGEN) - WGMS's selection includes additional criteria
        // ("primarily climate-driven... no major avalanche/calving/surge influence") that a purely
        // numeric years-and-gap rule can't see.
        var extraQualifying = qualifyingNames.Except(OfficialReferenceGlacierNames).ToList();
        Assert.HasCount(22, extraQualifying);
        Assert.HasCount(82, qualifyingNames);
    }

    /// <summary>
    /// Isolates how much of the numeric gap survives once the glacier-list difference is removed, by
    /// feeding <see cref="WgmsGlacierMassBalanceSourceFileTransformer"/> - completely unmodified - only
    /// the real rows for WGMS's own 61 official reference glaciers (every one of which trivially passes
    /// its existing "more than 10 years" Benchmark filter, since all of them have 30+), and comparing its
    /// output against WGMS's own published regional average for the same years. See the full-year table
    /// in the notes doc for every year, not just the sample below.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_RealDataRestrictedToOfficialReferenceGlaciers_StillDivergesFromWgmsRegionalAverage()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateWgmsZipFixture(input, LoadRealMassBalanceRows(OfficialReferenceGlacierNames));

        await new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        var actual = ParseYearValueCsv(output);
        var wgmsRegionAverage = LoadMbRefRegionAverageInMetres();

        // Even with EXACTLY WGMS's own reference-glacier list, the current per-glacier
        // anomaly-from-own-mean/flat-average method diverges from WGMS's raw/regional-average figure by
        // several hundred mm w.e. across the record (mean 0.53 m w.e. across all 74 comparable years -
        // see the notes doc) - the glacier list only accounts for part of the numeric gap between the two
        // series; most of it comes from the two methods measuring genuinely different things.
        foreach (var year in SampleYears)
        {
            var diff = Math.Abs(actual[year] - wgmsRegionAverage[year]);
            Assert.IsGreaterThan(
                0.1,
                diff,
                $"year {year}: expected current method's output ({actual[year]:0.000}) to diverge from " +
                $"WGMS's regional average ({wgmsRegionAverage[year]:0.000}) by more than 0.1 m w.e. (actual " +
                $"diff {diff:0.000}) - if this now fails, the current method may have converged with WGMS's.");
        }
    }

    /// <summary>
    /// Confirms the mechanism behind the remaining gap in the previous test: WGMS averages each glacier's
    /// raw (non-anomaly) annual balance within its GTN-G region first, then averages those per-region
    /// means (one value per region), rather than flat-averaging every glacier's anomaly together -
    /// "Global values are calculated using only one single value (averaged) for each region with glaciers
    /// to avoid a bias to well-observed regions" (https://wgms.ch/global-glacier-state/). This prototype
    /// is deliberately NOT wired into <see cref="WgmsGlacierMassBalanceSourceFileTransformer"/> - it
    /// exists only to explain the numeric gap, not to propose replacing the transformer's own metric.
    /// </summary>
    [TestMethod]
    public void RawTwoStageRegionalAverage_OfOfficialReferenceGlaciers_ReproducesWgmsPublishedIndex()
    {
        var byGlacierId = LoadRealAnnualBalances();
        var regionOfGlacierId = LoadGlacierRegions();
        var wgmsRegionAverage = LoadMbRefRegionAverageInMetres();

        var byYearThenRegion = new Dictionary<int, Dictionary<string, List<double>>>();
        foreach (var (glacierId, glacier) in byGlacierId)
        {
            if (!OfficialReferenceGlacierNames.Contains(glacier.Name))
            {
                continue;
            }

            var region = regionOfGlacierId[glacierId];
            foreach (var (year, value) in glacier.Years)
            {
                if (!byYearThenRegion.TryGetValue(year, out var regions))
                {
                    byYearThenRegion[year] = regions = [];
                }

                if (!regions.TryGetValue(region, out var values))
                {
                    regions[region] = values = [];
                }

                values.Add(value);
            }
        }

        foreach (var year in SampleYears)
        {
            var regionMeans = byYearThenRegion[year].Values.Select(values => values.Average());
            var globalMean = regionMeans.Average();

            // A ±0.05 m w.e. tolerance comfortably covers this prototype's residual difference from
            // WGMS's own published figure across the sampled years (observed max ~0.02 m w.e.) - the
            // small remainder is consistent with rounding in WGMS's own pipeline, not a methodology gap.
            Assert.AreEqual(wgmsRegionAverage[year], globalMean, 0.05, $"year {year}");
        }
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

    private static Dictionary<string, string> LoadGlacierRegions()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GlacierFixtures", "glacier_regions.csv");
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();

        var result = new Dictionary<string, string>();
        while (csv.Read())
        {
            result[csv.GetField("glacier_id")!] = csv.GetField("gtng_region")!;
        }

        return result;
    }

    private static Dictionary<string, (string Name, Dictionary<int, double> Years)> LoadRealAnnualBalances()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GlacierFixtures", "mass_balance_all_glaciers.csv");
        using var reader = new StreamReader(path);
        var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null };
        using var csv = new CsvReader(reader, csvConfiguration);
        csv.Read();
        csv.ReadHeader();

        var result = new Dictionary<string, (string Name, Dictionary<int, double> Years)>();
        while (csv.Read())
        {
            var glacierId = csv.GetField("glacier_id");
            var annualBalance = csv.GetField("annual_balance");
            if (string.IsNullOrWhiteSpace(glacierId) || string.IsNullOrWhiteSpace(annualBalance))
            {
                continue;
            }

            var year = csv.GetField<int>("year");
            var value = double.Parse(annualBalance, CultureInfo.InvariantCulture);

            if (!result.TryGetValue(glacierId, out var glacier))
            {
                result[glacierId] = glacier = (csv.GetField("glacier_name")!, []);
            }

            glacier.Years[year] = value;
        }

        return result;
    }

    /// <summary>Real "glacier_id,year,annual_balance" rows for glaciers whose glacier_name is in <paramref name="glacierNames"/>.</summary>
    private static List<string> LoadRealMassBalanceRows(HashSet<string> glacierNames)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GlacierFixtures", "mass_balance_all_glaciers.csv");
        using var reader = new StreamReader(path);
        var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null };
        using var csv = new CsvReader(reader, csvConfiguration);
        csv.Read();
        csv.ReadHeader();

        var rows = new List<string>();
        while (csv.Read())
        {
            var name = csv.GetField("glacier_name");
            if (name == null || !glacierNames.Contains(name))
            {
                continue;
            }

            var glacierId = csv.GetField("glacier_id");
            var year = csv.GetField("year");
            var annualBalance = csv.GetField("annual_balance");
            rows.Add($"{glacierId},{year},{annualBalance}");
        }

        return rows;
    }

    private static void CreateWgmsZipFixture(string zipPath, IEnumerable<string> rows)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("data/mass_balance.csv");
        using var writer = new StreamWriter(entry.Open());
        writer.WriteLine("glacier_id,year,annual_balance");
        foreach (var row in rows)
        {
            writer.WriteLine(row);
        }
    }
}
