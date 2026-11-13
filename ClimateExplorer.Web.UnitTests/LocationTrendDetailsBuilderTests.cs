namespace ClimateExplorer.Web.UnitTests;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.UnitTests.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LocationTrendDetailsBuilderTests
{
    [TestMethod]
    public void Build_NullDataRecords_ReturnsNoWindows()
    {
        var details = LocationTrendDetailsBuilder.Build(null);

        Assert.AreEqual(0, details.CompleteYearCount);
        Assert.IsEmpty(details.Windows);
    }

    [TestMethod]
    public void Build_29CompleteYears_ReturnsNoWindowsWithYearCount()
    {
        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(TrendTestData.CreateNoisyLine(1960, 29)));

        Assert.AreEqual(29, details.CompleteYearCount);
        Assert.HasCount(29, details.FullPeriodPoints);
        Assert.IsEmpty(details.Windows);
    }

    [TestMethod]
    public void Build_45CompleteYearsBefore1961_ReturnsFullPeriodOnly()
    {
        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(TrendTestData.CreateNoisyLine(1900, 45)));

        Assert.AreEqual(45, details.CompleteYearCount);
        Assert.HasCount(1, details.Windows);
        Assert.AreEqual(TrendWindow.Full, details.Windows[0].Window);
        Assert.HasCount(45, details.Windows[0].Points);
    }

    [TestMethod]
    public void Build_60CompleteYearsFrom1960_ReturnsEveryWindowInDashboardOrder()
    {
        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(TrendTestData.CreateNoisyLine(1960, 60)));

        CollectionAssert.AreEqual(
            new[] { TrendWindow.Last30, TrendWindow.Since1961, TrendWindow.Full, TrendWindow.FirstHalf },
            details.Windows.Select(x => x.Window).ToArray());

        var last30 = details.Windows[0];
        Assert.AreEqual(1990, last30.FirstYear);
        Assert.AreEqual(2019, last30.LastYear);
        Assert.HasCount(30, last30.Points);

        var since1961 = details.Windows[1];
        Assert.AreEqual(1961, since1961.FirstYear);
        Assert.AreEqual(2019, since1961.LastYear);
        Assert.HasCount(59, since1961.Points);

        var full = details.Windows[2];
        Assert.AreEqual(1960, full.FirstYear);
        Assert.AreEqual(2019, full.LastYear);
        Assert.HasCount(60, full.Points);

        var firstHalf = details.Windows[3];
        Assert.AreEqual(1960, firstHalf.FirstYear);
        Assert.AreEqual(1989, firstHalf.LastYear);
        Assert.HasCount(30, firstHalf.Points);
    }

    [TestMethod]
    public void Build_IncompleteYears_AreExcluded()
    {
        var records = ToBinnedRecords(TrendTestData.CreateNoisyLine(1960, 60));
        records.Insert(10, new BinnedRecord("y1900", null));
        records.Add(new BinnedRecord("y2020", null));

        var details = LocationTrendDetailsBuilder.Build(records);

        Assert.AreEqual(60, details.CompleteYearCount);
        var full = details.Windows.Single(x => x.Window == TrendWindow.Full);
        Assert.AreEqual(1960, full.FirstYear);
        Assert.AreEqual(2019, full.LastYear);
    }

    [TestMethod]
    public void Build_SameSeries_MatchesTheLocationsPreCalculatedSummaries()
    {
        var points = TrendTestData.CreateNoisyLine(1910, 115);
        var summaries = TemperatureTrendCalculator.Summarise(TemperatureTrendCalculator.Fit(points))!;

        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(points));

        Assert.IsTrue(summaries.Any(x => x.Window == TrendWindow.Since1961));

        foreach (var summary in summaries)
        {
            var window = details.Windows.Single(x => x.Window == summary.Window);
            Assert.AreEqual(summary.FirstYear, window.FirstYear);
            Assert.AreEqual(summary.Slope, window.Regression.Curve.Slope, 1e-5);
            Assert.AreEqual(summary.IsSignificant, window.IsSignificant);
        }
    }

    private static List<BinnedRecord> ToBinnedRecords(IEnumerable<DataPoint> points)
    {
        return [.. points.Select(x => new BinnedRecord($"y{x.X}", x.Y))];
    }
}
