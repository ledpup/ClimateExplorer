namespace ClimateExplorer.Core.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.UnitTests.TestData.TrendTestData;

[TestClass]
public class TemperatureTrendCalculatorTests
{
    [TestMethod]
    public void Fit_29CompleteYears_ReturnsNull()
    {
        Assert.IsNull(TemperatureTrendCalculator.Fit(CreateNoisyLine(1960, 29)));
    }

    [TestMethod]
    public void Fit_30CompleteYears_ReturnsFullPeriodOnly()
    {
        var fits = TemperatureTrendCalculator.Fit(CreateNoisyLine(1960, 30));

        Assert.IsNotNull(fits);
        Assert.AreEqual(30, fits.FullPeriod.Input.Count);
        Assert.IsNull(fits.Last30Years);
        Assert.IsNull(fits.FirstHalf);
    }

    [TestMethod]
    public void Fit_59CompleteYears_ReturnsFullPeriodOnly()
    {
        var fits = TemperatureTrendCalculator.Fit(CreateNoisyLine(1960, 59));

        Assert.IsNotNull(fits);
        Assert.AreEqual(59, fits.FullPeriod.Input.Count);
        Assert.IsNull(fits.Last30Years);
        Assert.IsNull(fits.FirstHalf);
    }

    [TestMethod]
    public void Fit_60CompleteYears_ReturnsFullLast30AndEarlyPeriod()
    {
        var fits = TemperatureTrendCalculator.Fit(CreateNoisyLine(1960, 60));

        Assert.IsNotNull(fits);
        Assert.AreEqual(60, fits.FullPeriod.Input.Count);
        Assert.IsNotNull(fits.Last30Years);
        Assert.AreEqual(TrendDefaults.RecentWindowYears, fits.Last30Years.Input.Count);
        Assert.AreEqual(2019 - 29, fits.Last30Years.Input.MinimumX);
        Assert.AreEqual(2019, fits.Last30Years.Input.MaximumX);
        Assert.IsNotNull(fits.FirstHalf);
        Assert.AreEqual(30, fits.FirstHalf.Input.Count);
        Assert.AreEqual(1960, fits.FirstHalf.Input.MinimumX);
        Assert.AreEqual(1989, fits.FirstHalf.Input.MaximumX);
    }

    [TestMethod]
    public void Fit_UnorderedPoints_Last30IsMostRecent()
    {
        var points = CreateNoisyLine(1900, 100).OrderBy(x => -x.X).ToList();

        var fits = TemperatureTrendCalculator.Fit(points);

        Assert.IsNotNull(fits);
        Assert.AreEqual(1970, fits.Last30Years!.Input.MinimumX);
        Assert.AreEqual(1999, fits.Last30Years.Input.MaximumX);
        Assert.AreEqual(1900, fits.OrderedPoints[0].X);
    }

    [TestMethod]
    public void Fit_KnownSeries_MatchesPolynomialRegressionCalculator()
    {
        var points = CreateNoisyLine(1900, 100);

        var fits = TemperatureTrendCalculator.Fit(points)!;
        var expectedFull = PolynomialRegressionCalculator.Calculate(points, degree: 1);
        var expectedLast30 = PolynomialRegressionCalculator.Calculate(points.TakeLast(30), degree: 1);
        var expectedFirstHalf = PolynomialRegressionCalculator.Calculate(points.Take(50), degree: 1);

        Assert.AreEqual(expectedFull.Curve.Slope, fits.FullPeriod.Curve.Slope, 1e-12);
        Assert.AreEqual(expectedFull.Significance.PValue, fits.FullPeriod.Significance.PValue, 1e-12);
        Assert.AreEqual(expectedLast30.Curve.Slope, fits.Last30Years!.Curve.Slope, 1e-12);
        Assert.AreEqual(expectedFirstHalf.Curve.Slope, fits.FirstHalf!.Curve.Slope, 1e-12);
    }

    [TestMethod]
    public void GetWindowPoints_OddCount_SlicesEachWindowFromTheOrderedPoints()
    {
        var ordered = CreateNoisyLine(1900, 61);

        var full = TemperatureTrendCalculator.GetWindowPoints(ordered, TrendWindow.Full);
        var last30 = TemperatureTrendCalculator.GetWindowPoints(ordered, TrendWindow.Last30);
        var firstHalf = TemperatureTrendCalculator.GetWindowPoints(ordered, TrendWindow.FirstHalf);

        Assert.HasCount(61, full);
        CollectionAssert.AreEqual(ordered.TakeLast(30).ToList(), last30.ToList());
        CollectionAssert.AreEqual(ordered.Take(30).ToList(), firstHalf.ToList());
    }

    [TestMethod]
    public void GetWindowPoints_RecentDecade_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            TemperatureTrendCalculator.GetWindowPoints(CreateNoisyLine(1900, 61), TrendWindow.RecentDecade));
    }

    [TestMethod]
    public void ToDataPoints_NullValues_SkipsIncompleteYears()
    {
        var chartable = new[]
        {
            new ChartableDataPoint { BinId = "y2000", Value = 10 },
            new ChartableDataPoint { BinId = "y2001", Value = null },
            new ChartableDataPoint { BinId = "y2002", Value = double.NaN },
            new ChartableDataPoint { BinId = "y2003", Value = 11 },
        };
        var binned = new[]
        {
            new BinnedRecord("y2000", 10),
            new BinnedRecord("y2001", null),
            new BinnedRecord("y2003", 11),
        };

        var fromChartable = TemperatureTrendCalculator.ToDataPoints(chartable);
        var fromBinned = TemperatureTrendCalculator.ToDataPoints(binned);

        CollectionAssert.AreEqual(new[] { new DataPoint(2000, 10), new DataPoint(2003, 11) }, fromChartable);
        CollectionAssert.AreEqual(new[] { new DataPoint(2000, 10), new DataPoint(2003, 11) }, fromBinned);
    }

    [TestMethod]
    public void Summarise_60CompleteYears_MapsWindowFirstYearSlopeAndSignificance()
    {
        var fits = TemperatureTrendCalculator.Fit(CreateNoisyLine(1960, 60))!;

        var summaries = TemperatureTrendCalculator.Summarise(fits);

        // The early period is fitted (for the modal) but deliberately not summarised onto Location.
        Assert.IsNotNull(fits.FirstHalf);
        Assert.IsNotNull(summaries);
        Assert.HasCount(2, summaries);
        Assert.AreEqual(TrendWindow.Full, summaries[0].Window);
        Assert.AreEqual(1960, summaries[0].FirstYear);
        Assert.AreEqual(fits.FullPeriod.Curve.Slope, summaries[0].Slope, 1e-5);
        Assert.AreEqual(fits.FullPeriod.Significance.IsSlopeSignificant, summaries[0].IsSignificant);
        Assert.AreEqual(TrendWindow.Last30, summaries[1].Window);
        Assert.AreEqual(1990, summaries[1].FirstYear);
        Assert.AreEqual(fits.Last30Years!.Curve.Slope, summaries[1].Slope, 1e-5);
        Assert.AreEqual(fits.Last30Years.Significance.IsSlopeSignificant, summaries[1].IsSignificant);
    }

    [TestMethod]
    public void Summarise_NullFits_ReturnsNull()
    {
        Assert.IsNull(TemperatureTrendCalculator.Summarise(null));
    }

    [TestMethod]
    public void Summarise_ConstantSeries_IsNotSignificantAndSerialises()
    {
        var points = Enumerable.Range(1900, 70).Select(year => new DataPoint(year, 15)).ToList();

        var summaries = TemperatureTrendCalculator.Summarise(TemperatureTrendCalculator.Fit(points));

        // A flat record has no trend to report, and the cache serialises with default options, which
        // throw on NaN/Infinity - a degenerate series must never put one into the summary.
        Assert.IsNotNull(summaries);
        Assert.IsTrue(summaries.All(x => x.Slope == 0 && !x.IsSignificant));
        _ = JsonSerializer.Serialize(summaries);
    }

    [TestMethod]
    public void SetTemperatureTrends_60CompleteYears_SetsTrendsAndLastYear()
    {
        var location = CreateLocation();

        TemperatureTrendCalculator.SetTemperatureTrends(location, CreateChartableLine(1960, 60));

        Assert.HasCount(2, location.TemperatureTrends!);
        Assert.AreEqual(2019, location.LastYear);
    }

    [TestMethod]
    public void SetTemperatureTrends_29CompleteYears_LeavesTrendsAndLastYearNull()
    {
        var location = CreateLocation();

        TemperatureTrendCalculator.SetTemperatureTrends(location, CreateChartableLine(1960, 29));

        Assert.IsNull(location.TemperatureTrends);
        Assert.IsNull(location.LastYear);
    }

    [TestMethod]
    public void SetTemperatureTrends_IncompleteYearAtTheEnd_LastYearIsLastCompleteYear()
    {
        var location = CreateLocation();
        var dataPoints = CreateChartableLine(1960, 40)
            .Append(new ChartableDataPoint { BinId = "y2000", Value = null })
            .ToList();

        TemperatureTrendCalculator.SetTemperatureTrends(location, dataPoints);

        Assert.AreEqual(1999, location.LastYear);
    }

    private static List<ChartableDataPoint> CreateChartableLine(int startYear, int count)
    {
        return [.. CreateNoisyLine(startYear, count).Select(x => new ChartableDataPoint { BinId = $"y{x.X}", Value = x.Y })];
    }
}
