namespace ClimateExplorer.Core.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TrendWindowCalculatorTests
{
    [TestMethod]
    public void Calculate_FewerThanMinimumPoints_ReturnsNull()
    {
        var points = CreatePerfectLine(0, 5, slope: 1);

        var result = TrendWindowCalculator.Calculate(points, minimumCompletePoints: 6, recentWindowSize: 30);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Calculate_ExactlyMinimumPoints_ReturnsNonNullResult()
    {
        var points = CreatePerfectLine(0, 6, slope: 1);

        var result = TrendWindowCalculator.Calculate(points, minimumCompletePoints: 6, recentWindowSize: 30);

        Assert.IsNotNull(result);
        Assert.AreEqual(6, result.CompletePointCount);
    }

    [TestMethod]
    public void Calculate_UnsortedInput_SortsBeforeWindowing()
    {
        var ordered = CreatePerfectLine(2000, 60, slope: 0.5);
        var shuffled = ordered.OrderBy(p => -p.X).ToList();

        var sortedResult = TrendWindowCalculator.Calculate(ordered, minimumCompletePoints: 60, recentWindowSize: 30);
        var shuffledResult = TrendWindowCalculator.Calculate(shuffled, minimumCompletePoints: 60, recentWindowSize: 30);

        Assert.IsNotNull(sortedResult);
        Assert.IsNotNull(shuffledResult);
        Assert.AreEqual(sortedResult.HistoricalTrend.Curve.Slope, shuffledResult.HistoricalTrend.Curve.Slope, 1e-9);
        Assert.AreEqual(sortedResult.RecentTrend.Curve.Slope, shuffledResult.RecentTrend.Curve.Slope, 1e-9);
        Assert.AreEqual(sortedResult.FirstHalfTrend.Curve.Slope, shuffledResult.FirstHalfTrend.Curve.Slope, 1e-9);
    }

    [TestMethod]
    public void Calculate_RecentWindowLargerThanAvailablePoints_ClampsToAvailableCount()
    {
        var points = CreatePerfectLine(2000, 10, slope: 1);

        var result = TrendWindowCalculator.Calculate(points, minimumCompletePoints: 6, recentWindowSize: 30);

        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.RecentTrend.Input.Count);
    }

    [TestMethod]
    public void Calculate_SixtyPointPerfectLine_AllThreeWindowsReportTheSameSlope()
    {
        var points = CreatePerfectLine(1960, 60, slope: 0.1);

        var result = TrendWindowCalculator.Calculate(points, minimumCompletePoints: 60, recentWindowSize: 30);

        Assert.IsNotNull(result);
        Assert.AreEqual(0.1, result.HistoricalTrend.Curve.Slope, 1e-9);
        Assert.AreEqual(0.1, result.RecentTrend.Curve.Slope, 1e-9);
        Assert.AreEqual(0.1, result.FirstHalfTrend.Curve.Slope, 1e-9);
    }

    [TestMethod]
    public void Calculate_FirstHalfDiffersFromRecentWindow_ReportsDifferentSlopesPerWindow()
    {
        var points = new List<DataPoint>();
        for (var year = 1960; year < 1990; year++)
        {
            points.Add(new DataPoint(year, 0));
        }

        for (var year = 1990; year < 2020; year++)
        {
            points.Add(new DataPoint(year, (year - 1990) * 2));
        }

        var result = TrendWindowCalculator.Calculate(points, minimumCompletePoints: 60, recentWindowSize: 30);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(result.FirstHalfTrend.Curve.Slope, result.RecentTrend.Curve.Slope);
        Assert.AreEqual(0d, result.FirstHalfTrend.Curve.Slope, 1e-9);
        Assert.AreEqual(2d, result.RecentTrend.Curve.Slope, 1e-9);
    }

    [TestMethod]
    public void Calculate_MinimumCompletePointsBelowSix_Throws()
    {
        var points = CreatePerfectLine(2000, 10, slope: 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            TrendWindowCalculator.Calculate(points, minimumCompletePoints: 5, recentWindowSize: 30));
    }

    [TestMethod]
    public void Calculate_PointsFrom1900_FitsSince1961OverTheYearsFrom1961()
    {
        var points = CreatePerfectLine(1900, 121, slope: 0.02);

        var result = TrendWindowCalculator.Calculate(points, minimumCompletePoints: 60, recentWindowSize: 30)!;

        Assert.IsNotNull(result.Since1961Trend);
        Assert.AreEqual(1961, result.Since1961Trend.Input.MinimumX);
        Assert.AreEqual(60, result.Since1961Trend.Input.Count);
        Assert.AreEqual(0.02, result.Since1961Trend.Curve.Slope, 1e-9);
    }

    [TestMethod]
    public void Calculate_PointsStartIn1966_Since1961IsNull()
    {
        var points = CreatePerfectLine(1966, 60, slope: 0.02);

        var result = TrendWindowCalculator.Calculate(points, minimumCompletePoints: 60, recentWindowSize: 30)!;

        Assert.IsNull(result.Since1961Trend);
    }

    private static List<DataPoint> CreatePerfectLine(int startYear, int count, double slope)
    {
        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, slope * i))];
    }
}
