namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LatestRecordComparisonTests
{
    [TestMethod]
    public void RankCalculatesWarmSidePercentileAndAnomaly()
    {
        var result = LatestRecordComparison.Rank(14d, [10d, 11d, 12d, 13d, 15d])!;

        Assert.AreEqual(2, result.HighRank);
        Assert.AreEqual(5, result.LowRank);
        Assert.AreEqual(66.67d, result.HighPercentile, 0.01d);
        Assert.AreEqual(1.8d, result.Anomaly, 0.01d);
        Assert.AreEqual(LatestRecordComparisonDirection.High, result.Direction);
    }

    [TestMethod]
    public void TemperatureHeadlineUsesOrdinalForVeryWarmPeriods()
    {
        var result = LatestRecordComparison.Rank(19d, [10d, 11d, 12d, 13d, 20d])!;

        var headline = LatestRecordComparison.BuildTemperatureHeadline("May", result);

        Assert.AreEqual("2nd warmest May on record", headline);
    }

    [TestMethod]
    public void PrecipitationHeadlineAvoidsDriestRecordForCommonTiedZeroRain()
    {
        var result = LatestRecordComparison.Rank(0d, [0d, 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d])!;

        var headline = LatestRecordComparison.BuildPrecipitationHeadline("11 June", result);

        Assert.AreEqual("Drier than usual", headline);
    }

    [TestMethod]
    public void PercentileSentenceUsesDominantDryDirection()
    {
        var result = LatestRecordComparison.Rank(2d, [0d, 1d, 3d, 4d, 5d, 6d, 7d, 8d, 9d])!;

        var sentence = LatestRecordComparison.BuildPrecipitationPercentileSentence(1910, result);

        Assert.AreEqual("Drier than 70% of comparable periods since 1910", sentence);
    }

    [TestMethod]
    [DataRow(1, "1st")]
    [DataRow(2, "2nd")]
    [DataRow(3, "3rd")]
    [DataRow(4, "4th")]
    [DataRow(11, "11th")]
    [DataRow(12, "12th")]
    [DataRow(13, "13th")]
    [DataRow(21, "21st")]
    public void FormatOrdinalHandlesEnglishSuffixes(int value, string expected)
    {
        Assert.AreEqual(expected, LatestRecordComparison.FormatOrdinal(value));
    }
}
