using ClimateExplorer.Core.Stats.Smoothing;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class SeriesSmoothingOptionsExtensionsTests
{
    [TestMethod]
    public void CreateSmoother_CentredMovingAverage_ReturnsCentredCalculator()
    {
        Assert.IsInstanceOfType<CentredMovingAverageCalculator>(SeriesSmoothingOptions.CentredMovingAverage.CreateSmoother());
    }

    [TestMethod]
    public void CreateSmoother_LocalLinearRegression_ReturnsLocalLinearRegressionCalculator()
    {
        Assert.IsInstanceOfType<LocalLinearRegressionCalculator>(SeriesSmoothingOptions.LocalLinearRegression.CreateSmoother());
    }

    [TestMethod]
    [DataRow(SeriesSmoothingOptions.None)]
    [DataRow(SeriesSmoothingOptions.Trendline)]
    public void CreateSmoother_OptionWithoutWindow_ReturnsNull(SeriesSmoothingOptions option)
    {
        Assert.IsNull(option.CreateSmoother());
        Assert.IsFalse(option.UsesWindow());
    }
}
