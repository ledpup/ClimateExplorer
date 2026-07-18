namespace ClimateExplorer.UnitTests;

using System;
using System.IO;
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
}
