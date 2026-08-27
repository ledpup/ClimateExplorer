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
    // years, no gap in the 2016-2025 recent-decade window). annual_balance(G_i, year) = i + (year - 2020):
    // each glacier's own mean is exactly its offset `i` (the (year - 2020) deltas are symmetric around
    // 2020 and sum to zero), so every glacier's anomaly for a given year is exactly (year - 2020) -
    // letting the expected averaged output be computed by hand.
    private static readonly string[] WgmsExpectedIndexLines =
    [
        "Year,Value",
        "2015,-5.000",
        "2016,-4.000",
        "2017,-3.000",
        "2018,-2.000",
        "2019,-1.000",
        "2020,0.000",
        "2021,1.000",
        "2022,2.000",
        "2023,3.000",
        "2024,4.000",
        "2025,5.000",
    ];

    [TestMethod]
    public async Task TransformAsync_WgmsRows_AveragesAnomaliesAcrossBenchmarkGlaciers()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateWgmsFixture(input, BenchmarkGlacierRows());

        await new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(WgmsExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsGlaciersFailingBenchmarkRule_AreExcludedFromIndex()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = BenchmarkGlacierRows().ToList();

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

        await new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        // Neither disqualified glacier's outlier values shift the index away from the 5-benchmark-glacier result.
        CollectionAssert.AreEqual(WgmsExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsYearBelowMinimumContributingGlaciers_IsDroppedFromIndex()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = BenchmarkGlacierRows().ToList();

        // A sparse extra year (2005), reported by only 4 of the 5 benchmark glaciers (G1-G4), each at a
        // value equal to that glacier's existing mean (its offset `i`) - this doesn't shift any glacier's
        // own mean (so every other year's anomaly is unaffected), but 2005 itself has only 4 contributing
        // glaciers, below the minimum of 5, so it must be dropped rather than appear as a noisy single year.
        for (var offset = 1; offset <= 4; offset++)
        {
            rows.Add($"G{offset},2005,{offset}");
        }

        CreateWgmsFixture(input, rows);

        await new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(WgmsExpectedIndexLines, await File.ReadAllLinesAsync(output));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsUnparseableRows_AreSkippedWithoutAffectingIndex()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        var rows = BenchmarkGlacierRows().ToList();
        rows.Add("GARBAGE1,2030,"); // blank annual_balance
        rows.Add("GARBAGE2,2030,N/A"); // non-numeric annual_balance
        rows.Add(",2030,1.5"); // blank glacier_id
        rows.Add("GARBAGE3,not-a-year,1.5"); // non-numeric year

        CreateWgmsFixture(input, rows);

        await new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(WgmsExpectedIndexLines, await File.ReadAllLinesAsync(output));
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
            () => new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsArchiveWithNoAnnualBalanceValues_ThrowsInvalidDataException()
    {
        var input = Path.Combine(temporaryRoot, "input.zip");
        var output = Path.Combine(temporaryRoot, "output.csv");
        CreateWgmsFixture(input, ["G1,2020,", "G1,2021,"]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None));
    }

    [TestMethod]
    public async Task TransformAsync_WgmsArchiveWithNoBenchmarkGlaciers_ThrowsInvalidDataException()
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
            () => new WgmsGlacierMassBalanceSourceFileTransformer().TransformAsync(input, output, CancellationToken.None));
    }

    private static IEnumerable<string> BenchmarkGlacierRows()
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

    private static void CreateWgmsFixture(string zipPath, IEnumerable<string> rows)
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
