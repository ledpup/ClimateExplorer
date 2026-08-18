namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ChartSeriesTrendCalculatorTests
{
    private static readonly TrendStatSubject Subject = new("Maximum temperature", "°C");

    [TestMethod]
    public void Calculate_FewerYearsThanTheMinimum_ReportsUnavailableAndProjectsNothing()
    {
        var points = CreatePerfectLine(1960, ChartSeriesTrendCalculator.MinimumYearsForTrend - 1, slope: 0.02);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 20);

        Assert.IsNotNull(result.UnavailableReason);
        Assert.IsEmpty(result.Windows);
        Assert.IsNull(result.Projection);
        Assert.IsFalse(result.HasSignificantWindow);
    }

    [TestMethod]
    public void Calculate_ExactlyTheMinimumYears_FitsEveryWindow()
    {
        var points = CreatePerfectLine(1960, ChartSeriesTrendCalculator.MinimumYearsForTrend, slope: 0.02);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 20);

        Assert.IsNull(result.UnavailableReason);
        Assert.HasCount(4, result.Windows);
        Assert.IsNotNull(result.GetWindow(TrendWindow.Full));
        Assert.IsNotNull(result.GetWindow(TrendWindow.Recent));
        Assert.IsNotNull(result.GetWindow(TrendWindow.RecentDecade));
        Assert.IsNotNull(result.GetWindow(TrendWindow.FirstHalf));
    }

    [TestMethod]
    public void Calculate_WindowsAreInDropdownDisplayOrder()
    {
        var points = CreatePerfectLine(1960, ChartSeriesTrendCalculator.MinimumYearsForTrend, slope: 0.02);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 20);

        CollectionAssert.AreEqual(
            new[] { TrendWindow.Full, TrendWindow.Recent, TrendWindow.RecentDecade, TrendWindow.FirstHalf },
            result.Windows.Select(x => x.Window).ToList());
    }

    [TestMethod]
    public void Calculate_EarlyPeriodWindow_MatchesTrendWindowCalculatorFirstHalf()
    {
        // The chart's "early period" must stay the same thing the Recent Observations tab calls the
        // first half - the first half of the available points, not the first half of the calendar
        // span and not the first 30 years.
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.FirstHalf, predictionYears: 5);
        var reference = TrendWindowCalculator.Calculate(
            points,
            ChartSeriesTrendCalculator.MinimumYearsForTrend,
            ChartSeriesTrendCalculator.RecentWindowYears);

        var earlyPeriod = result.GetWindow(TrendWindow.FirstHalf)!;

        Assert.IsNotNull(reference);
        Assert.AreEqual(reference.FirstHalfTrend.Curve.Slope, earlyPeriod.Regression.Curve.Slope, 1e-12);
        Assert.HasCount(101 / 2, earlyPeriod.Points);
        Assert.AreEqual(1900, earlyPeriod.FirstYear);
        Assert.AreEqual(1949, earlyPeriod.LastYear);
    }

    [TestMethod]
    public void Calculate_RecentWindow_UsesTheLastThirtyYears()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Recent, predictionYears: 5);
        var recent = result.GetWindow(TrendWindow.Recent)!;

        Assert.HasCount(ChartSeriesTrendCalculator.RecentWindowYears, recent.Points);
        Assert.AreEqual(1971, recent.FirstYear);
        Assert.AreEqual(2000, recent.LastYear);
    }

    [TestMethod]
    public void Calculate_RecentDecadeWindow_UsesTheLastTenYearsWithTheSameLogicAsRecentWindow()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.RecentDecade, predictionYears: 5);
        var recentDecade = result.GetWindow(TrendWindow.RecentDecade)!;

        Assert.HasCount(ChartSeriesTrendCalculator.RecentDecadeWindowYears, recentDecade.Points);
        Assert.AreEqual(1991, recentDecade.FirstYear);
        Assert.AreEqual(2000, recentDecade.LastYear);

        // Same underlying logic as the last-30-years window (the last N points, then an OLS fit
        // over just that subset) - a perfectly linear series should agree on slope regardless of
        // which of the two "last N years" windows fitted it.
        var recent = result.GetWindow(TrendWindow.Recent)!;
        Assert.AreEqual(recent.Regression.Curve.Slope, recentDecade.Regression.Curve.Slope, 1e-9);
    }

    [TestMethod]
    public void Calculate_SignificantTrend_ProjectsFromTheYearAfterTheLastDataYear()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 10);

        Assert.IsNotNull(result.Projection);
        Assert.AreEqual(2000, result.LastDataYear);
        Assert.HasCount(10, result.Projection!.Predictions);
        Assert.AreEqual(2001, result.Projection.FirstYear);
        Assert.AreEqual(2010, result.Projection.LastYear);
    }

    [TestMethod]
    public void Calculate_SignificantTrend_ProjectedValuesFollowTheFittedLine()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 5);
        var regression = result.GetWindow(TrendWindow.Full)!.Regression;

        foreach (var prediction in result.Projection!.Predictions)
        {
            Assert.AreEqual(regression.Curve.Predict(prediction.X), prediction.PredictedY, 1e-9);
        }
    }

    [TestMethod]
    public void Calculate_SignificantTrend_CarriesBothIntervalBoundsForEachProjectedYear()
    {
        // The interval bounds aren't rendered yet, but they are computed now so the prediction-band
        // overlay doesn't need the data model to change.
        var points = CreateNoisyLine(1900, 101, slope: 0.03, noise: 0.2);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 5);

        foreach (var prediction in result.Projection!.Predictions)
        {
            Assert.IsLessThan(prediction.PredictedY, prediction.MeanConfidenceInterval.Lower);
            Assert.IsGreaterThan(prediction.PredictedY, prediction.MeanConfidenceInterval.Upper);

            // The observation interval also allows for year-to-year scatter, so it is always wider.
            Assert.IsLessThan(prediction.MeanConfidenceInterval.Lower, prediction.ObservationPredictionInterval.Lower);
            Assert.IsGreaterThan(prediction.MeanConfidenceInterval.Upper, prediction.ObservationPredictionInterval.Upper);
        }
    }

    [TestMethod]
    public void Calculate_NoSignificantWindow_ProjectsNothingButStillReportsEveryWindow()
    {
        var points = CreateFlatNoise(1900, 101);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 20);

        Assert.IsNull(result.UnavailableReason);
        Assert.HasCount(4, result.Windows);
        Assert.IsFalse(result.HasSignificantWindow);
        Assert.IsEmpty(result.SignificantWindows);
        Assert.IsNull(result.Projection);
    }

    [TestMethod]
    public void Calculate_PredictionYearsAboveTheMaximum_ClampsToTheMaximum()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 5000);

        Assert.HasCount(TrendPredictionRange.Maximum, result.Projection!.Predictions);
    }

    [TestMethod]
    public void Calculate_PredictionYearsBelowTheMinimum_ClampsToTheMinimum()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 0);

        Assert.HasCount(TrendPredictionRange.Minimum, result.Projection!.Predictions);
    }

    [TestMethod]
    public void Calculate_PredictionTargetYearGiven_ProjectsToThatYearRegardlessOfPredictionYears()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        // predictionYears would give 5 predictions on its own; predictionTargetYear must win.
        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 5, predictionTargetYear: 2100);

        Assert.AreEqual(2000, result.LastDataYear);
        Assert.HasCount(100, result.Projection!.Predictions);
        Assert.AreEqual(2001, result.Projection.FirstYear);
    }

    [TestMethod]
    public void Calculate_PredictionTargetYearStaysPinnedAsTheRecordGrows_UnlikeAPlainDuration()
    {
        // Simulates the same preset ("project to 2100") evaluated against two builds of the same
        // series a year apart - the fixed target keeps landing on 2100 regardless of how much more
        // data has arrived, which is exactly what a plain TrendPredictionYears duration cannot do.
        var earlierBuild = CreatePerfectLine(1900, 101, slope: 0.03); // last year 2000
        var laterBuild = CreatePerfectLine(1900, 102, slope: 0.03); // last year 2001

        var earlierResult = ChartSeriesTrendCalculator.Calculate(Subject, earlierBuild, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 50, predictionTargetYear: 2100);
        var laterResult = ChartSeriesTrendCalculator.Calculate(Subject, laterBuild, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 50, predictionTargetYear: 2100);

        Assert.AreEqual(2100, earlierResult.LastDataYear + earlierResult.Projection!.Predictions.Count);
        Assert.AreEqual(2100, laterResult.LastDataYear + laterResult.Projection!.Predictions.Count);
    }

    [TestMethod]
    public void Calculate_PredictionTargetYearImpliesMoreThanTheMaximumDuration_ClampsToTheMaximum()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03);

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 20, predictionTargetYear: 9999);

        Assert.HasCount(TrendPredictionRange.Maximum, result.Projection!.Predictions);
    }

    [TestMethod]
    public void Calculate_UnsortedPoints_ProjectsFromTheLatestYearRegardless()
    {
        var points = CreatePerfectLine(1900, 101, slope: 0.03).OrderBy(x => -x.X).ToList();

        var result = ChartSeriesTrendCalculator.Calculate(Subject, points, TrendRegressionType.Linear, TrendWindow.Full, predictionYears: 3);

        Assert.AreEqual(2000, result.LastDataYear);
        Assert.AreEqual(2001, result.Projection!.FirstYear);
    }

    [TestMethod]
    public void ResolveWindow_RequestedWindowIsSignificant_KeepsTheRequestedWindow()
    {
        var significant = new[] { TrendWindow.Full, TrendWindow.Recent };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, TrendWindow.Recent);

        Assert.AreEqual(TrendWindow.Recent, result);
    }

    [TestMethod]
    public void ResolveWindow_RequestedWindowIsNotSignificant_FallsBackInPriorityOrder()
    {
        var significant = new[] { TrendWindow.FirstHalf, TrendWindow.Recent };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, TrendWindow.Full);

        Assert.AreEqual(TrendWindow.Recent, result);
    }

    [TestMethod]
    public void ResolveWindow_NothingRequested_PrefersTheFullPeriod()
    {
        var significant = new[] { TrendWindow.FirstHalf, TrendWindow.Recent, TrendWindow.Full, TrendWindow.RecentDecade };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, requestedWindow: null);

        Assert.AreEqual(TrendWindow.Full, result);
    }

    [TestMethod]
    public void ResolveWindow_FullPeriodNotSignificant_PrefersTheRecentPeriodNext()
    {
        var significant = new[] { TrendWindow.FirstHalf, TrendWindow.Recent, TrendWindow.RecentDecade };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, requestedWindow: null);

        Assert.AreEqual(TrendWindow.Recent, result);
    }

    [TestMethod]
    public void ResolveWindow_FullAndRecentPeriodsNotSignificant_PrefersTheRecentDecadeNext()
    {
        var significant = new[] { TrendWindow.FirstHalf, TrendWindow.RecentDecade };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, requestedWindow: null);

        Assert.AreEqual(TrendWindow.RecentDecade, result);
    }

    [TestMethod]
    public void ResolveWindow_NoSignificantWindows_ReturnsNull()
    {
        var result = ChartSeriesTrendCalculator.ResolveWindow([], TrendWindow.Full);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ResolveWindow_NothingRequestedAndFullPeriodExcluded_SkipsToTheNextPriorityWindow()
    {
        // Simulates a second trend being added to a series whose first trend already shows Full -
        // the auto-pick should prefer something new rather than a duplicate.
        var significant = new[] { TrendWindow.Full, TrendWindow.Recent, TrendWindow.RecentDecade };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, requestedWindow: null, excludedFromDefault: new HashSet<TrendWindow> { TrendWindow.Full });

        Assert.AreEqual(TrendWindow.Recent, result);
    }

    [TestMethod]
    public void ResolveWindow_ExplicitRequestMatchesAnExcludedWindow_IsStillHonoured()
    {
        // The exclusion set only steers the *fallback* pick - an explicit user choice (e.g. from
        // the dropdown, or already set from a URL) is never second-guessed, even if it collides
        // with another trend on the same series.
        var significant = new[] { TrendWindow.Full, TrendWindow.Recent };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, TrendWindow.Full, excludedFromDefault: new HashSet<TrendWindow> { TrendWindow.Full });

        Assert.AreEqual(TrendWindow.Full, result);
    }

    [TestMethod]
    public void ResolveWindow_EveryHigherPriorityWindowExcluded_FallsBackToARepeatRatherThanNull()
    {
        // When every significant window is already claimed by another trend on the series, a
        // repeat is preferred over reporting "no significant trend" when one genuinely exists.
        var significant = new[] { TrendWindow.Full };

        var result = ChartSeriesTrendCalculator.ResolveWindow(significant, requestedWindow: null, excludedFromDefault: new HashSet<TrendWindow> { TrendWindow.Full });

        Assert.AreEqual(TrendWindow.Full, result);
    }

    private static List<DataPoint> CreatePerfectLine(int startYear, int count, double slope)
    {
        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, 10 + (slope * i)))];
    }

    private static List<DataPoint> CreateNoisyLine(int startYear, int count, double slope, double noise)
    {
        var random = new Random(20260812);

        return
        [
            .. Enumerable
                .Range(0, count)
                .Select(i => new DataPoint(startYear + i, 10 + (slope * i) + ((random.NextDouble() - 0.5) * noise))),
        ];
    }

    private static List<DataPoint> CreateFlatNoise(int startYear, int count)
    {
        // Alternating values around a constant mean: plenty of scatter, no underlying trend.
        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, 10 + (i % 2 == 0 ? 1 : -1)))];
    }
}
