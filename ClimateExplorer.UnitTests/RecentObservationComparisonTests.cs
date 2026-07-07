namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RecentObservationComparisonTests
{
    [TestMethod]
    public void RankCalculatesWarmSidePercentileAndAnomaly()
    {
        var result = RecentObservationComparison.Rank(14d, [10d, 11d, 12d, 13d, 15d])!;

        Assert.AreEqual(2, result.HighRank);
        Assert.AreEqual(5, result.LowRank);
        Assert.AreEqual(66.67d, result.HighPercentile, 0.01d);
        Assert.AreEqual(15d, result.HistoricalMax);
        Assert.AreEqual(10d, result.HistoricalMin);
        Assert.AreEqual(1.8d, result.Anomaly, 0.01d);
        Assert.AreEqual(RecentObservationComparisonDirection.High, result.Direction);
    }

    [TestMethod]
    public void TemperatureHeadlineUsesOrdinalForVeryWarmPeriods()
    {
        var result = RecentObservationComparison.Rank(19d, [10d, 11d, 12d, 13d, 20d])!;

        var headline = RecentObservationComparison.BuildTemperatureHeadline("May", result);

        Assert.AreEqual("2nd warmest May", headline);
    }

    [TestMethod]
    public void PrecipitationHeadlineAvoidsDriestRecordForCommonTiedZeroRain()
    {
        var result = RecentObservationComparison.Rank(0d, [0d, 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d])!;

        var headline = RecentObservationComparison.BuildPrecipitationHeadline("11 June", result);

        Assert.AreEqual("Drier than usual", headline);
    }

    [TestMethod]
    public void PercentileSentenceUsesDominantDryDirection()
    {
        var result = RecentObservationComparison.Rank(2d, [0d, 1d, 3d, 4d, 5d, 6d, 7d, 8d, 9d])!;

        var sentence = RecentObservationComparison.BuildPrecipitationPercentileSentence(1910, result);

        Assert.AreEqual("Drier than 70% of comparable periods", sentence);
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
        Assert.AreEqual(expected, RecentObservationComparison.FormatOrdinal(value));
    }
}
