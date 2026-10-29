namespace ClimateExplorer.Web.UnitTests;

using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TrendPredictionRangeTests
{
    [TestMethod]
    public void TryParsePredictUntilYear_YearInRange_ReturnsTrueAndYear()
    {
        var parsed = TrendPredictionRange.TryParsePredictUntilYear("2075", anchorYear: 2025, out var targetYear);

        Assert.IsTrue(parsed);
        Assert.AreEqual(2075, targetYear);
    }

    [TestMethod]
    public void TryParsePredictUntilYear_NonNumeric_ReturnsFalse()
    {
        var parsed = TrendPredictionRange.TryParsePredictUntilYear("soon", anchorYear: 2025, out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void TryParsePredictUntilYear_YearOutOfRange_ReturnsFalse()
    {
        var parsed = TrendPredictionRange.TryParsePredictUntilYear("2126", anchorYear: 2025, out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void TryParsePredictUntilYear_AnchorYear_ReturnsFalse()
    {
        // Zero years past the end of the data is below the one-year minimum
        var parsed = TrendPredictionRange.TryParsePredictUntilYear("2025", anchorYear: 2025, out _);

        Assert.IsFalse(parsed);
    }
}
