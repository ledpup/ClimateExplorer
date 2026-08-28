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
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataSetSourceFileTransformerTests
{
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerTransformerTests-{Guid.NewGuid():N}");
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

    [TestMethod]
    public async Task TransformAsync_OceanAcidityRows_AveragesValidValuesByMonth()
    {
        var input = Path.Combine(temporaryRoot, "input.txt");
        var output = Path.Combine(temporaryRoot, "output.csv");
        await File.WriteAllLinesAsync(
            input,
            [
                "metadata",
                "cruise\tdays\tdate\tpHcalc_25C\tnotes",
                "1\t1\t1-Jan-20\t8.0\ta",
                "2\t2\t15-Jan-20\t8.2\tb",
                "3\t3\t1-Feb-20\t-999\tc",
                "4\t4\t1-Mar-20\t8.05\td",
            ]);

        await new OceanAciditySourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[]
            {
                "Year,Month,Calculated pH at 25°C",
                "2020,1,8.1",
                "2020,3,8.05",
            },
            await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_SeaLevelRows_AveragesAvailableSatelliteValues()
    {
        var input = Path.Combine(temporaryRoot, "input.csv");
        var output = Path.Combine(temporaryRoot, "output.csv");
        await File.WriteAllLinesAsync(
            input,
            [
                "#title = fixture",
                "year,satellite-1,satellite-2",
                "2020.50000,1.000,3.000",
                "2021.00000,,4.000",
            ]);

        await new SeaLevelSourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[]
            {
                "#title = fixture",
                "year,sea-level [mm]",
                "2020-07-02,2.000",
                "2021-01-01,4.000",
            },
            await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_OzoneRows_AveragesEveryDayIncludingLastDay()
    {
        var input = Path.Combine(temporaryRoot, "input.csv");
        var output = Path.Combine(temporaryRoot, "output.csv");
        await File.WriteAllLinesAsync(
            input,
            [
                "datetime,fixture value",
                "2020-01-01T00:00,1.000",
                "2020-01-01T12:00,3.000",
                "2020-01-02T00:00,5.000",
            ]);

        await new OzoneSourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[]
            {
                "datetime,fixture value",
                "2020-01-01,2.000",
                "2020-01-02,5.000",
            },
            await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_OceanAcidityWithoutHeader_ThrowsInvalidDataException()
    {
        var input = Path.Combine(temporaryRoot, "input.txt");
        var output = Path.Combine(temporaryRoot, "output.csv");
        await File.WriteAllTextAsync(input, "unexpected content");

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => new OceanAciditySourceFileTransformer().TransformAsync(input, output, CancellationToken.None));
    }

    // Five "Benchmark" glaciers (G1-G5), each reporting 2015-2025 (11 years - qualifies: more than 10
    // years, no gap in the 2016-2025 recent-decade window). annual_balance(G_i, year) = i + (year - 2020),
    // so the one-stage (flat) average for a given year is just mean(1..5) + (year - 2020) = 3 + (year -
    // 2020) - the raw balance itself, not an anomaly, letting the expected output be computed by hand.
    private static readonly string[] OneStageExpectedIndexLines =
    [
        "Year,Value",
        "2015,-2.000",
        "2016,-1.000",
        "2017,0.000",
        "2018,1.000",
        "2019,2.000",
        "2020,3.000",
        "2021,4.000",
        "2022,5.000",
        "2023,6.000",
        "2024,7.000",
        "2025,8.000",
    ];

    // Same 5 glaciers/values as above, but split into two GTN-G regions (G1-G3 in REGION_A, G4-G5 in
    // REGION_B). Region A's mean is mean(1,2,3) + offset = 2 + offset; region B's is mean(4,5) + offset =
    // 4.5 + offset; the two-stage global figure is the mean of those two region means: 3.25 + offset -
    // deliberately different from the one-stage flat average (3 + offset) above, since region B (2
    // glaciers) is weighted equally to region A (3 glaciers) rather than by glacier count.
    private static readonly string[] TwoStageExpectedIndexLines =
    [
        "Year,Value",
        "2015,-1.750",
        "2016,-0.750",
        "2017,0.250",
        "2018,1.250",
        "2019,2.250",
        "2020,3.250",
        "2021,4.250",
        "2022,5.250",
        "2023,6.250",
        "2024,7.250",
        "2025,8.250",
    ];

    [TestMethod]
    public async Task TransformAsync_WgmsRows_OneStageFlatAveragesRawBalanceAcrossBenchmarkGlaciers()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateWgmsFixture(input, FiveGlacierRows());

        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
            .TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(OneStageExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsRows_TwoStageAveragesRawBalanceByRegionThenGlobally()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateWgmsFixture(
            input,
            FiveGlacierRows(),
            ["G1,REGION_A", "G2,REGION_A", "G3,REGION_A", "G4,REGION_B", "G5,REGION_B"]);

        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.TwoStage)
            .TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(TwoStageExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsGlaciersFailingBenchmarkRule_AreExcludedFromIndex()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = FiveGlacierRows().ToList();

        // G6: only 5 years of records (2021-2025) - fails "more than 10 years", despite huge outlier values.
        for (var year = 2021; year <= 2025; year++)
        {
            rows.Add($"G6,{year},999");
        }

        // G7: 11 years of records (2010-2020, so "more than 10 years" passes), but only 5 of those fall in
        // the 2016-2025 recent-decade window - a 5-year gap fails "at most one year's gap".
        for (var year = 2010; year <= 2020; year++)
        {
            rows.Add($"G7,{year},-999");
        }

        CreateWgmsFixture(input, rows);

        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
            .TransformAsync(input, output, CancellationToken.None);

        // Neither disqualified glacier's outlier values shift the index away from the 5-benchmark-glacier result.
        CollectionAssert.AreEqual(OneStageExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsReferenceFilter_ExcludesGlaciersWithThirtyOrFewerYears()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = new List<string>();

        // G1-G5: 31 years each (1994-2024, constant value = glacier's offset) - qualifies both Benchmark
        // (>10 years) and Reference (>30 years), and covers the 2015-2024 recent-decade window with no gap.
        for (var offset = 1; offset <= 5; offset++)
        {
            for (var year = 1994; year <= 2024; year++)
            {
                rows.Add($"G{offset},{year},{offset.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        // G6: only 15 years (2010-2024, constant value 100) - qualifies Benchmark (>10) but not Reference
        // (15 is not more than 30), despite fully covering the recent-decade window with no gap.
        for (var year = 2010; year <= 2024; year++)
        {
            rows.Add($"G6,{year},100");
        }

        CreateWgmsFixture(input, rows);

        // Reference: G6 never qualifies, so every year is just mean(1,2,3,4,5) = 3, throughout 1994-2024.
        var referenceOutput = Path.Combine(temporaryRoot, "reference-output.csv");
        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Reference, WgmsAveragingStage.OneStage)
            .TransformAsync(input, referenceOutput, CancellationToken.None);
        var expectedReference = new List<string> { "Year,Value" };
        for (var year = 1994; year <= 2024; year++)
        {
            expectedReference.Add($"{year},3.000");
        }

        CollectionAssert.AreEqual(expectedReference, await File.ReadAllLinesAsync(referenceOutput));

        // Benchmark: G6 also qualifies, so 2010-2024 (where it contributes) averages in its outlier value:
        // mean(1,2,3,4,5,100) = 115/6 = 19.167. Years before G6 reports (1994-2009) are unaffected.
        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
            .TransformAsync(input, output, CancellationToken.None);
        var expectedBenchmark = new List<string> { "Year,Value" };
        for (var year = 1994; year <= 2009; year++)
        {
            expectedBenchmark.Add($"{year},3.000");
        }

        for (var year = 2010; year <= 2024; year++)
        {
            expectedBenchmark.Add($"{year},19.167");
        }

        CollectionAssert.AreEqual(expectedBenchmark, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsYearBelowMinimumContributingGlaciers_IsDroppedFromIndex()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = FiveGlacierRows().ToList();

        // A sparse extra year (2005), reported by only 4 of the 5 benchmark glaciers (G1-G4). 2005 has
        // only 4 contributing glaciers, below the minimum of 5, so it must be dropped rather than appear
        // as a noisy single year - regardless of the values reported (arbitrary here, and irrelevant).
        for (var offset = 1; offset <= 4; offset++)
        {
            rows.Add($"G{offset},2005,{offset}");
        }

        CreateWgmsFixture(input, rows);

        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
            .TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(OneStageExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsUnparseableRows_AreSkippedWithoutAffectingIndex()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = FiveGlacierRows().ToList();
        rows.Add("GARBAGE1,2030,"); // blank annual_balance
        rows.Add("GARBAGE2,2030,N/A"); // non-numeric annual_balance
        rows.Add(",2030,1.5"); // blank glacier_id
        rows.Add("GARBAGE3,not-a-year,1.5"); // non-numeric year

        CreateWgmsFixture(input, rows);

        await new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
            .TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(OneStageExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsArchiveMissingMassBalanceEntry_ThrowsInvalidDataException()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        using (var archive = ZipFile.Open(input, ZipArchiveMode.Create))
        {
            archive.CreateEntry("data/glacier.csv");
        }

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
                .TransformAsync(input, output, CancellationToken.None));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsTwoStageArchiveMissingGlacierEntry_ThrowsInvalidDataException()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateWgmsFixture(input, FiveGlacierRows()); // no glacier.csv entry

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.TwoStage)
                .TransformAsync(input, output, CancellationToken.None));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsTwoStageQualifyingGlacierMissingFromGlacierCsv_ThrowsInvalidDataException()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");

        // glacier.csv is present, but doesn't have a row for G5 (one of the five qualifying glaciers).
        CreateWgmsFixture(input, FiveGlacierRows(), ["G1,REGION_A", "G2,REGION_A", "G3,REGION_A", "G4,REGION_B"]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.TwoStage)
                .TransformAsync(input, output, CancellationToken.None));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsArchiveWithNoAnnualBalanceValues_ThrowsInvalidDataException()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateWgmsFixture(input, ["G1,2020,", "G1,2021,"]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
                .TransformAsync(input, output, CancellationToken.None));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsArchiveWithNoQualifyingGlaciers_ThrowsInvalidDataException()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = new List<string>();
        for (var year = 2020; year <= 2025; year++)
        {
            rows.Add($"G1,{year},1.0"); // only 6 years - fails "more than 10 years"
        }

        CreateWgmsFixture(input, rows);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.OneStage)
                .TransformAsync(input, output, CancellationToken.None));
    }

    private static IEnumerable<string> FiveGlacierRows()
    {
        for (var offset = 1; offset <= 5; offset++)
        {
            for (var year = 2015; year <= 2025; year++)
            {
                var value = offset + (year - 2020);
                yield return $"G{offset},{year},{value.ToString(CultureInfo.InvariantCulture)}";
            }
        }
    }

    /// <summary>Writes data/mass_balance.csv, plus data/glacier.csv (as "id,gtng_region" rows) when <paramref name="glacierRegionRows"/> is supplied.</summary>
    private static void CreateWgmsFixture(string zipPath, IEnumerable<string> massBalanceRows, IEnumerable<string>? glacierRegionRows = null)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        var massBalanceEntry = archive.CreateEntry("data/mass_balance.csv");
        using (var writer = new StreamWriter(massBalanceEntry.Open()))
        {
            writer.WriteLine("glacier_id,year,annual_balance");
            foreach (var row in massBalanceRows)
            {
                writer.WriteLine(row);
            }
        }

        if (glacierRegionRows != null)
        {
            var glacierEntry = archive.CreateEntry("data/glacier.csv");
            using var writer = new StreamWriter(glacierEntry.Open());
            writer.WriteLine("id,gtng_region");
            foreach (var row in glacierRegionRows)
            {
                writer.WriteLine(row);
            }
        }
    }
}
