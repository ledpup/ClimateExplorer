namespace ClimateExplorer.UnitTests;

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
        Assert.AreEqual(sortedResult.HistoricalTrend.Line.Slope, shuffledResult.HistoricalTrend.Line.Slope, 1e-9);
        Assert.AreEqual(sortedResult.RecentTrend.Line.Slope, shuffledResult.RecentTrend.Line.Slope, 1e-9);
        Assert.AreEqual(sortedResult.FirstHalfTrend.Line.Slope, shuffledResult.FirstHalfTrend.Line.Slope, 1e-9);
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
        Assert.AreEqual(0.1, result.HistoricalTrend.Line.Slope, 1e-9);
        Assert.AreEqual(0.1, result.RecentTrend.Line.Slope, 1e-9);
        Assert.AreEqual(0.1, result.FirstHalfTrend.Line.Slope, 1e-9);
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
        Assert.AreNotEqual(result.FirstHalfTrend.Line.Slope, result.RecentTrend.Line.Slope);
        Assert.AreEqual(0d, result.FirstHalfTrend.Line.Slope, 1e-9);
        Assert.AreEqual(2d, result.RecentTrend.Line.Slope, 1e-9);
    }

    [TestMethod]
    public void Calculate_MinimumCompletePointsBelowSix_Throws()
    {
        var points = CreatePerfectLine(2000, 10, slope: 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            TrendWindowCalculator.Calculate(points, minimumCompletePoints: 5, recentWindowSize: 30));
    }

    private static List<DataPoint> CreatePerfectLine(int startYear, int count, double slope)
    {
        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, slope * i))];
    }
}
