namespace ClimateExplorer.UnitTests;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class TrendUnitsTests
{
    [TestMethod]
    public void FormatAmountPerDecade_DegreesFahrenheit_ScaledWithTwoDecimalsWithoutSpace()
    {
        // 0.032°C a year is 0.32°C a decade, which is 0.576°F: a rate is a difference, never offset.
        Assert.AreEqual("+0.58°F", LocationTrendFieldBuilder.FormatAmountPerDecade(0.032, UnitFormatter.UsCustomary));
        Assert.AreEqual("-0.38°F", LocationTrendFieldBuilder.FormatAmountPerDecade(-0.021, UnitFormatter.UsCustomary));
    }

    [TestMethod]
    public void FormatAmountPerDecade_Metric_Unchanged()
    {
        Assert.AreEqual("+0.32°C", LocationTrendFieldBuilder.FormatAmountPerDecade(0.032, UnitFormatter.Metric));
        Assert.AreEqual("+0.32°C", LocationTrendFieldBuilder.FormatAmountPerDecade(0.032));
    }

    [TestMethod]
    public void Build_UsCustomary_FieldAndTooltipInFahrenheit()
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Last30, 1995, 0.032, true)];
        location.LastYear = 2024;

        var field = LocationTrendFieldBuilder.Build(location, UnitFormatter.UsCustomary);

        Assert.AreEqual("+0.58°F /decade", field!.Value);
        StringAssert.Contains(field.Tooltip, "+0.58°F /decade");
        Assert.DoesNotContain("°C", field.Tooltip);
    }

    [TestMethod]
    public void NotificationBuild_InchesSubject_RatesInInches()
    {
        var subject = new TrendStatSubject("Precipitation", UnitOfMeasure.Millimetres);
        var trend = ChartSeriesTrendCalculator.Calculate(subject, CreateNoise(1900, 101), TrendRegressionType.Linear, requestedWindow: null, predictionYears: 20);

        var usCustomary = ChartSeriesTrendNotificationBuilder.Build("Hobart | Rainfall", trend, "Hobart", null, units: UnitFormatter.UsCustomary);
        var metric = ChartSeriesTrendNotificationBuilder.Build("Hobart | Rainfall", trend, "Hobart", null);

        StringAssert.Contains(usCustomary!.Message, " in /decade");
        StringAssert.Contains(metric!.Message, "mm /decade");
        Assert.DoesNotContain("mm /decade", usCustomary.Message);
    }

    [TestMethod]
    public void NotificationBuild_SubjectThatCannotConvert_StaysMetric()
    {
        var subject = new TrendStatSubject("Summed temperature", UnitOfMeasure.DegreesCelsius, CanConvert: false);
        var trend = ChartSeriesTrendCalculator.Calculate(subject, CreateNoise(1900, 101), TrendRegressionType.Linear, requestedWindow: null, predictionYears: 20);

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Summed temperature", trend, "Hobart", null, units: UnitFormatter.UsCustomary);

        StringAssert.Contains(notification!.Message, "°C /decade");
    }

    [TestMethod]
    public void TrendStatSubject_MetricUnitLabel_AlwaysMetric()
    {
        Assert.AreEqual("°C", new TrendStatSubject("Temperature", UnitOfMeasure.DegreesCelsius).MetricUnitLabel);
        Assert.AreEqual(string.Empty, new TrendStatSubject("Frost days", null).MetricUnitLabel);
    }

    private static List<DataPoint> CreateNoise(int startYear, int count)
    {
        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, 600 + (i % 2 == 0 ? 50 : -50)))];
    }
}
