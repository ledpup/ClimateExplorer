namespace ClimateExplorer.UnitTests;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.Trends;
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
        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(TemperatureTrendCalculatorTests.CreateNoisyLine(1960, 29)));

        Assert.AreEqual(29, details.CompleteYearCount);
        Assert.HasCount(29, details.FullPeriodPoints);
        Assert.IsEmpty(details.Windows);
    }

    [TestMethod]
    public void Build_45CompleteYears_ReturnsFullPeriodOnly()
    {
        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(TemperatureTrendCalculatorTests.CreateNoisyLine(1960, 45)));

        Assert.AreEqual(45, details.CompleteYearCount);
        Assert.HasCount(1, details.Windows);
        Assert.AreEqual(TrendWindow.Full, details.Windows[0].Window);
        Assert.HasCount(45, details.Windows[0].Points);
    }

    [TestMethod]
    public void Build_60CompleteYears_ReturnsFullLast30AndEarlyPeriodInOrder()
    {
        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(TemperatureTrendCalculatorTests.CreateNoisyLine(1960, 60)));

        CollectionAssert.AreEqual(
            new[] { TrendWindow.Full, TrendWindow.Last30, TrendWindow.FirstHalf },
            details.Windows.Select(x => x.Window).ToArray());

        var full = details.Windows[0];
        Assert.AreEqual(1960, full.FirstYear);
        Assert.AreEqual(2019, full.LastYear);
        Assert.HasCount(60, full.Points);

        var last30 = details.Windows[1];
        Assert.AreEqual(1990, last30.FirstYear);
        Assert.AreEqual(2019, last30.LastYear);
        Assert.HasCount(30, last30.Points);

        var firstHalf = details.Windows[2];
        Assert.AreEqual(1960, firstHalf.FirstYear);
        Assert.AreEqual(1989, firstHalf.LastYear);
        Assert.HasCount(30, firstHalf.Points);
    }

    [TestMethod]
    public void Build_IncompleteYears_AreExcluded()
    {
        var records = ToBinnedRecords(TemperatureTrendCalculatorTests.CreateNoisyLine(1960, 60));
        records.Insert(10, new BinnedRecord("y1900", null));
        records.Add(new BinnedRecord("y2020", null));

        var details = LocationTrendDetailsBuilder.Build(records);

        Assert.AreEqual(60, details.CompleteYearCount);
        Assert.AreEqual(1960, details.Windows[0].FirstYear);
        Assert.AreEqual(2019, details.Windows[0].LastYear);
    }

    [TestMethod]
    public void Build_SameSeries_MatchesTheLocationsPreCalculatedSummaries()
    {
        var points = TemperatureTrendCalculatorTests.CreateNoisyLine(1910, 115);
        var summaries = TemperatureTrendCalculator.Summarise(TemperatureTrendCalculator.Fit(points))!;

        var details = LocationTrendDetailsBuilder.Build(ToBinnedRecords(points));

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
