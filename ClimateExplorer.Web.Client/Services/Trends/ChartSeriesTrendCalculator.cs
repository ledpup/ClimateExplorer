namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// Fits the three trend windows for one chart series and projects the selected one forward.
/// </summary>
/// <remarks>
/// The windows, the minimum data requirement and the significance test are all deliberately the
/// same as the Recent Observations trend tab's, via the shared
/// <see cref="TrendWindowCalculator"/>, so the chart and the tile never disagree about whether a
/// location has a trend. All three windows are fitted in one pass because the dropdown can only
/// offer the significant ones, and the About-trends panel shows the statistics for all three
/// regardless.
/// </remarks>
public static class ChartSeriesTrendCalculator
{
    /// <summary>
    /// Matches the Recent Observations trend tab: the same 60-year floor used for the warming
    /// anomaly and the heating score, so short records don't produce headline long-term trends.
    /// </summary>
    public const int MinimumYearsForTrend = AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly;

    public const int RecentWindowYears = 30;

    /// <summary>
    /// Which window to fall back to when the user hasn't chosen one, or when the one they chose is
    /// no longer significant. Most-recently-relevant first, since a trend module switched on for
    /// the first time is more often asked "what's happening lately?" than "what's the whole record
    /// done?".
    /// </summary>
    private static readonly TrendWindow[] SelectionPriority = [TrendWindow.Recent, TrendWindow.Full, TrendWindow.FirstHalf];

    public static ChartSeriesTrend Calculate(
        TrendStatSubject subject,
        IReadOnlyList<DataPoint> points,
        TrendWindow? requestedWindow,
        int predictionYears)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(points);

        var ordered = points.OrderBy(x => x.X).ToList();

        var trendSet = ordered.Count >= MinimumYearsForTrend
            ? TrendWindowCalculator.Calculate(ordered, MinimumYearsForTrend, RecentWindowYears)
            : null;

        if (trendSet is null)
        {
            return new ChartSeriesTrend
            {
                Subject = subject,
                Points = ordered,
                UnavailableReason =
                    $"Only {ordered.Count} complete {(ordered.Count == 1 ? "year" : "years")} of data are plotted, and a trend needs at least "
                    + $"{MinimumYearsForTrend}. That minimum is used across the site (for example the warming anomaly and the heating "
                    + "score) so long-term trends aren't skewed by short records.",
                LastDataYear = ordered.Count == 0 ? 0 : (int)Math.Round(ordered[^1].X),
            };
        }

        var recentCount = Math.Min(RecentWindowYears, ordered.Count);
        var firstHalfCount = ordered.Count / 2;

        var windows = new List<ChartSeriesTrendWindowResult>
        {
            new(TrendWindow.Full, trendSet.HistoricalTrend, ordered),
            new(TrendWindow.Recent, trendSet.RecentTrend, [.. ordered.TakeLast(recentCount)]),
            new(TrendWindow.FirstHalf, trendSet.FirstHalfTrend, [.. ordered.Take(firstHalfCount)]),
        };

        var lastDataYear = (int)Math.Round(ordered[^1].X);
        var significantWindows = windows.Where(x => x.IsSignificant).Select(x => x.Window).ToList();
        var selectedWindow = ResolveWindow(significantWindows, requestedWindow);

        return new ChartSeriesTrend
        {
            Subject = subject,
            Windows = windows,
            Points = ordered,
            LastDataYear = lastDataYear,
            Projection = selectedWindow is null
                ? null
                : Project(
                    windows.Single(x => x.Window == selectedWindow.Value),
                    lastDataYear,
                    TrendPredictionRange.Clamp(predictionYears)),
        };
    }

    /// <summary>
    /// The window to display: the user's choice when it is still significant, otherwise the first
    /// significant one in priority order, otherwise nothing.
    /// </summary>
    public static TrendWindow? ResolveWindow(IReadOnlyList<TrendWindow> significantWindows, TrendWindow? requestedWindow)
    {
        ArgumentNullException.ThrowIfNull(significantWindows);

        if (requestedWindow.HasValue && significantWindows.Contains(requestedWindow.Value))
        {
            return requestedWindow;
        }

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
            .Select(year => LinearRegressionCalculator.Predict(window.Regression, year))
            .ToList();

        return new ChartSeriesTrendProjection(window.Window, predictions);
    }
}
