namespace ClimateExplorer.Web.UnitTests;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartSeriesTrendNotificationBuilderTests
{
    private static readonly TrendStatSubject Subject = new("Maximum temperature", UnitOfMeasure.DegreesCelsius);

    [TestMethod]
    public void Build_EveryWindowSignificant_ReturnsNoNotification()
    {
        var trend = Calculate(CreatePerfectLine(1900, 101, slope: 0.03));

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Max temperature", trend, "Hobart", null);

        Assert.IsNull(notification);
    }

    [TestMethod]
    public void Build_NoWindowSignificant_WarnsThatNoPeriodCanBeShown()
    {
        var trend = Calculate(CreateFlatNoise(1900, 101));

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Max temperature", trend, "Hobart", null);

        Assert.IsNotNull(notification);
        Assert.AreEqual(NotificationType.Warning, notification!.Type);
        StringAssert.Contains(notification.Message, "none of the trend periods produce a statistically significant trend");
        StringAssert.Contains(notification.Message, "no trend line was added");
        Assert.AreEqual("Hobart", notification.LocationName);
    }

    [TestMethod]
    public void Build_NoWindowSignificant_ExplainsEachRejectedPeriodWithItsPValue()
    {
        var trend = Calculate(CreateFlatNoise(1900, 101));

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Max temperature", trend, "Hobart", null);

        Assert.IsNotNull(notification);
        StringAssert.Contains(notification!.Message, "Full period");
        StringAssert.Contains(notification.Message, "Last 30 years");
        StringAssert.Contains(notification.Message, "Last 10 years");
        StringAssert.Contains(notification.Message, "Early period");
        StringAssert.Contains(notification.Message, "p = ");
        StringAssert.Contains(notification.Message, "0.05");
    }

    [TestMethod]
    public void Build_SomeWindowsSignificant_InformsWhichPeriodsAreNotOffered()
    {
        // A flat early half followed by a strongly rising recent half: the full and recent windows
        // are significant, the early one is not.
        var points = new List<DataPoint>();
        for (var year = 1900; year < 1951; year++)
        {
            points.Add(new DataPoint(year, 10 + (year % 2 == 0 ? 0.5 : -0.5)));
        }

        for (var year = 1951; year <= 2000; year++)
        {
            points.Add(new DataPoint(year, 10 + ((year - 1950) * 0.05)));
        }

        var trend = Calculate(points);

        Assert.IsTrue(trend.HasSignificantWindow);
        Assert.IsFalse(trend.GetWindow(TrendWindow.FirstHalf)!.IsSignificant);

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Max temperature", trend, "Hobart", null);

        Assert.IsNotNull(notification);
        Assert.AreEqual(NotificationType.Info, notification!.Type);
        StringAssert.Contains(notification.Message, "Early period");
        StringAssert.Contains(notification.Message, "isn't offered as an option");
    }

    [TestMethod]
    public void Build_TooFewYears_WarnsThatNoTrendCouldBeFitted()
    {
        var trend = Calculate(CreatePerfectLine(1990, 10, slope: 0.03));

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Max temperature", trend, "Hobart", null);

        Assert.IsNotNull(notification);
        Assert.AreEqual(NotificationType.Warning, notification!.Type);
        StringAssert.Contains(notification.Message, "no trend could be fitted");
        StringAssert.Contains(notification.Message, ChartSeriesTrendCalculator.MinimumYearsForTrend.ToString());
    }

    [TestMethod]
    public void Build_RegressionTypeGiven_PrefixesTheMessageWithIt()
    {
        var trend = Calculate(CreateFlatNoise(1900, 101));

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Max temperature", trend, "Hobart", null, TrendRegressionType.Quadratic);

        Assert.IsNotNull(notification);
        StringAssert.StartsWith(notification!.Message, "<div>Hobart | Max temperature, <b>quadratic trend</b>:");
    }

    [TestMethod]
    public void Build_NoRegressionTypeGiven_PrefixesTheMessageWithTheSeriesTitleOnly()
    {
        var trend = Calculate(CreateFlatNoise(1900, 101));

        var notification = ChartSeriesTrendNotificationBuilder.Build("Hobart | Max temperature", trend, "Hobart", null);

        StringAssert.StartsWith(notification!.Message, "<div>Hobart | Max temperature:");
    }

    [TestMethod]
    public void BuildForSeries_TrendsShareARegressionType_NotifiesOnceWithoutNamingIt()
    {
        var points = CreateFlatNoise(1900, 101);
        List<ChartSeriesTrend> trends =
        [
            Calculate(points, TrendRegressionType.Linear, TrendWindow.Full),
            Calculate(points, TrendRegressionType.Linear, TrendWindow.Last30),
            Calculate(points, TrendRegressionType.Linear, TrendWindow.FirstHalf),
        ];

        var notifications = ChartSeriesTrendNotificationBuilder.BuildForSeries("Hobart | Max temperature", trends, "Hobart", null);

        Assert.HasCount(1, notifications);
        StringAssert.StartsWith(notifications[0].Message, "<div>Hobart | Max temperature:");
    }

    [TestMethod]
    public void BuildForSeries_TrendsOfDifferentRegressionTypes_NotifiesOncePerTypeAndNamesIt()
    {
        var points = CreateFlatNoise(1900, 101);
        List<ChartSeriesTrend> trends =
        [
            Calculate(points, TrendRegressionType.Linear, TrendWindow.Full),
            Calculate(points, TrendRegressionType.Quadratic, TrendWindow.Full),
            Calculate(points, TrendRegressionType.Linear, TrendWindow.Last30),
        ];

        var notifications = ChartSeriesTrendNotificationBuilder.BuildForSeries("Hobart | Max temperature", trends, "Hobart", null);

        Assert.HasCount(2, notifications);
        StringAssert.StartsWith(notifications[0].Message, "<div>Hobart | Max temperature, <b>linear trend</b>:");
        StringAssert.StartsWith(notifications[1].Message, "<div>Hobart | Max temperature, <b>quadratic trend</b>:");
    }

    [TestMethod]
    public void BuildForSeries_TooFewYearsForAnyTrend_NotifiesOnceWithoutNamingARegressionType()
    {
        var points = CreatePerfectLine(1990, 10, slope: 0.03);
        List<ChartSeriesTrend> trends =
        [
            Calculate(points, TrendRegressionType.Linear, TrendWindow.Full),
            Calculate(points, TrendRegressionType.Cubic, TrendWindow.Full),
        ];

        var notifications = ChartSeriesTrendNotificationBuilder.BuildForSeries("Hobart | Max temperature", trends, "Hobart", null);

        Assert.HasCount(1, notifications);
        Assert.AreEqual("Hobart | Max temperature: no trend could be fitted. " + trends[0].UnavailableReason, notifications[0].Message);
    }

    [TestMethod]
    public void BuildForSeries_EveryWindowSignificant_ReturnsNoNotifications()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);
        List<ChartSeriesTrend> trends =
        [
            Calculate(points, TrendRegressionType.Linear, TrendWindow.Full),
            Calculate(points, TrendRegressionType.Linear, TrendWindow.Last30),
        ];

        var notifications = ChartSeriesTrendNotificationBuilder.BuildForSeries("Hobart | Max temperature", trends, "Hobart", null);

        Assert.IsEmpty(notifications);
    }

    private static ChartSeriesTrend Calculate(
        IReadOnlyList<DataPoint> points,
        TrendRegressionType regressionType = TrendRegressionType.Linear,
        TrendWindow? requestedWindow = null)
    {
        return ChartSeriesTrendCalculator.Calculate(Subject, points, regressionType, requestedWindow, predictionYears: 20);
    }

    private static List<DataPoint> CreatePerfectLine(int startYear, int count, double slope)
    {
        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, 10 + (slope * i)))];
    }

    private static List<DataPoint> CreateFlatNoise(int startYear, int count)
    {
        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, 10 + (i % 2 == 0 ? 1 : -1)))];
    }
}
