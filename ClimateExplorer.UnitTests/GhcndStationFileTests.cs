namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Data.Ghcnd;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class GhcndStationFileTests
{
    // Verbatim rows from NOAA's ghcnd-stations.txt; the format is fixed-width, so the spacing is the data.
    private const string DeBilt = "NLM00006260  52.0989    5.1794    2.0    DE BILT                        GSN     06260";
    private const string StJohns = "ACW00011604  17.1167  -61.7833   10.1    ST JOHNS COOLIDGE FLD                       ";

    [TestMethod]
    public void TryParse_StationPublishingAWmoId_ReadsItFromTheDocumentedColumns()
    {
        Assert.IsTrue(GhcndStationFile.TryParse(DeBilt, out var row));

        Assert.AreEqual("NLM00006260", row.Id);
        Assert.AreEqual("DE BILT", row.Name);
        Assert.AreEqual(52.0989, row.Latitude, 0.00001);
        Assert.AreEqual(5.1794, row.Longitude, 0.00001);
        Assert.AreEqual("06260", row.WmoId);
    }

    [TestMethod]
    public void TryParse_StationWithoutAWmoId_LeavesItNullRatherThanBlank()
    {
        Assert.IsTrue(GhcndStationFile.TryParse(StJohns, out var row));

        Assert.AreEqual("ACW00011604", row.Id);
        Assert.AreEqual(-61.7833, row.Longitude, 0.00001);
        Assert.IsNull(row.WmoId);
    }

    [TestMethod]
    public void TryParse_LineThatIsNotAStationRow_IsRejectedRatherThanPartiallyRead()
    {
        Assert.IsFalse(GhcndStationFile.TryParse(string.Empty, out _));
        Assert.IsFalse(GhcndStationFile.TryParse("not a fixed width station row", out _));
    }
}
