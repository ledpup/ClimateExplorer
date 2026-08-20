namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// Fits the trend windows for one chart series, at the user's chosen regression shape, and projects
/// the selected one forward.
/// </summary>
/// <remarks>
/// The full-period, last-30-years, last-10-years and early-period windows, the minimum data
/// requirement and the significance test are all deliberately the same shape as the Recent
/// Observations trend tab's - same four window definitions, same significance rule generalised from
/// a slope t-test to an overall-model F-test (identical to the t-test at degree 1) - so the chart and
/// the tile never disagree about whether a location has a trend, when both are looking at a linear
/// fit. Unlike the tile, every window here can be fit at degree 1, 2 or 3
/// (<see cref="TrendRegressionType"/>): picking a curve can make a window newly significant or newly
/// not, so every window is fit directly with <see cref="PolynomialRegressionCalculator"/> at the
/// requested degree rather than through <c>TrendWindowCalculator</c>, which is fixed at degree 1 by
/// its contract with the Recent Observations tab (see the design doc). Every window is fitted in one
/// pass because the dropdown can only offer the significant ones, and the About-trends panel shows
/// the statistics for all of them regardless.
/// </remarks>
public static class ChartSeriesTrendCalculator
{
    /// <summary>
    /// Matches the Recent Observations trend tab: the same 60-year floor used for the warming
    /// anomaly and the heating score, so short records don't produce headline long-term trends.
    /// </summary>
    public const int MinimumYearsForTrend = AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly;

    public const int RecentWindowYears = 30;

    public const int RecentDecadeWindowYears = 10;

    /// <summary>
    /// Which window to fall back to when the user hasn't chosen one, or when the one they chose is
    /// no longer significant. Full period first, since that's the trend most people mean by default
    /// ("what's the whole record done?"), then most-recently-relevant.
    /// </summary>
    private static readonly TrendWindow[] SelectionPriority =
        [TrendWindow.Full, TrendWindow.Last30, TrendWindow.RecentDecade, TrendWindow.FirstHalf];

    /// <param name="lastMeasuredYear">
    /// The true last calendar year with real (raw) data, when it differs from the last year in
    /// <paramref name="points"/> - i.e. when <paramref name="points"/> is a smoothed series whose
    /// trailing years were trimmed by a centred moving average. Anchors both the projection's
    /// start year and <see cref="ChartSeriesTrend.LastDataYear"/>, so the trend module never draws
    /// a "projected" point for a year that's already been measured, just not plotted. Defaults to
    /// the last year in <paramref name="points"/> when omitted, which is correct whenever the
    /// series isn't smoothed (or the caller doesn't know the difference).
    /// </param>
    public static ChartSeriesTrend Calculate(
        TrendStatSubject subject,
        IReadOnlyList<DataPoint> points,
        TrendRegressionType regressionType,
        TrendWindow? requestedWindow,
        int predictionYears,
        int? predictionTargetYear = null,
        IReadOnlySet<TrendWindow>? excludedFromDefault = null,
        int? lastMeasuredYear = null)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(points);

        var ordered = points.OrderBy(x => x.X).ToList();

        if (ordered.Count < MinimumYearsForTrend)
        {
            return new ChartSeriesTrend
            {
                Subject = subject,
                RegressionType = regressionType,
                Points = ordered,
                UnavailableReason =
                    $"Only {ordered.Count} complete {(ordered.Count == 1 ? "year" : "years")} of data are plotted, and a trend needs at least "
                    + $"{MinimumYearsForTrend}. That minimum is used across the site (for example the warming anomaly and the heating "
                    + "score) so long-term trends aren't skewed by short records.",
                LastDataYear = lastMeasuredYear ?? (ordered.Count == 0 ? 0 : (int)Math.Round(ordered[^1].X)),
            };
        }

        var degree = (int)regressionType;
        var recentCount = Math.Min(RecentWindowYears, ordered.Count);
        var recentDecadeCount = Math.Min(RecentDecadeWindowYears, ordered.Count);
        var firstHalfCount = ordered.Count / 2;

        var recentPoints = ordered.TakeLast(recentCount).ToList();
        var recentDecadePoints = ordered.TakeLast(recentDecadeCount).ToList();
        var firstHalfPoints = ordered.Take(firstHalfCount).ToList();

        // Declared in dropdown/tab display order: full period first, matching SelectionPriority
        // below.
        var windows = new List<ChartSeriesTrendWindowResult>
        {
            new(TrendWindow.Full, PolynomialRegressionCalculator.Calculate(ordered, degree), ordered),
            new(TrendWindow.Last30, PolynomialRegressionCalculator.Calculate(recentPoints, degree), recentPoints),
            new(TrendWindow.RecentDecade, PolynomialRegressionCalculator.Calculate(recentDecadePoints, degree), recentDecadePoints),
            new(TrendWindow.FirstHalf, PolynomialRegressionCalculator.Calculate(firstHalfPoints, degree), firstHalfPoints),
        };

        // Ordinarily the same as the last fitted point, but a caller fitting a smoothed series
        // (a centred moving average trims the trailing years it can't fill a full window for)
        // passes the true last calendar year that has raw data, so the projection never starts
        // on a year that's already measured, just not plotted.
        var lastDataYear = lastMeasuredYear ?? (int)Math.Round(ordered[^1].X);
        var significantWindows = windows.Where(x => x.IsSignificant).Select(x => x.Window).ToList();
        var selectedWindow = ResolveWindow(significantWindows, requestedWindow, excludedFromDefault);

        // A fixed target year (e.g. a preset that always projects "to 2100") takes priority over
        // the duration and is resolved against *this* build's last data year, so the projection's
        // endpoint stays pinned even as the underlying record grows - unlike the duration, which
        // would otherwise carry the projection forward by however much new data has arrived.
        var effectivePredictionYears = TrendPredictionRange.Clamp(
            predictionTargetYear.HasValue ? predictionTargetYear.Value - lastDataYear : predictionYears);

        return new ChartSeriesTrend
        {
            Subject = subject,
            RegressionType = regressionType,
            Windows = windows,
            Points = ordered,
            LastDataYear = lastDataYear,
            Projection = selectedWindow is null
                ? null
                : Project(
                    windows.Single(x => x.Window == selectedWindow.Value),
                    lastDataYear,
                    effectivePredictionYears),
        };
    }

    /// <summary>
    /// The window to display: the user's choice when it is still significant, otherwise the first
    /// significant one in priority order, otherwise nothing.
    /// </summary>
    /// <param name="excludedFromDefault">
    /// Windows to skip when falling back to the priority order - used when a series already has
    /// another trend showing one of these windows, so a freshly-added trend tends to surface
    /// something new rather than a duplicate. Only applies to the fallback: an explicit
    /// <paramref name="requestedWindow"/> is always honoured even if it's in this set.
    /// </param>
    public static TrendWindow? ResolveWindow(
        IReadOnlyList<TrendWindow> significantWindows,
        TrendWindow? requestedWindow,
        IReadOnlySet<TrendWindow>? excludedFromDefault = null)
    {
        ArgumentNullException.ThrowIfNull(significantWindows);

        if (requestedWindow.HasValue && significantWindows.Contains(requestedWindow.Value))
        {
            return requestedWindow;
        }

        var preferred = SelectionPriority
            .Cast<TrendWindow?>()
            .FirstOrDefault(window => significantWindows.Contains(window!.Value) && excludedFromDefault?.Contains(window.Value) != true);

        if (preferred.HasValue)
        {
            return preferred;
        }

        // Every significant window is already shown by another trend on this series - fall back to
        // the ordinary priority order (allowing a repeat) rather than reporting "no significant
        // trend" when a significant one does exist.
        return SelectionPriority
            .Cast<TrendWindow?>()
            .FirstOrDefault(window => significantWindows.Contains(window!.Value));
    }

    private static ChartSeriesTrendProjection Project(
        ChartSeriesTrendWindowResult window,
        int lastDataYear,
        int predictionYears)
    {
        var predictions = Enumerable
            .Range(lastDataYear + 1, predictionYears)
            .Select(year => PolynomialRegressionCalculator.Predict(window.Regression, year))
            .ToList();

        return new ChartSeriesTrendProjection(window.Window, predictions);
    }
}
