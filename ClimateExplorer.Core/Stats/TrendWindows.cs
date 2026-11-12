namespace ClimateExplorer.Core.Stats;

using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// Window rules shared by the location trends, the chart and Recent Observations, so each one fits the
/// same years.
/// </summary>
public static class TrendWindows
{
    /// <summary>
    /// The points of the Since 1961 window: every point from <see cref="TrendDefaults.FixedPeriodStartYear"/>
    /// on. Null when the window isn't eligible: its first point isn't within
    /// <see cref="TrendDefaults.FixedPeriodStartToleranceYears"/> years of the start (1961-1965), or it has
    /// fewer than <see cref="TrendDefaults.MinimumYearsForTrend"/> points.
    /// </summary>
    /// <param name="orderedPoints">Yearly points (X is the year), in year order.</param>
    public static IReadOnlyList<DataPoint>? GetSince1961Points(IReadOnlyList<DataPoint> orderedPoints)
    {
        ArgumentNullException.ThrowIfNull(orderedPoints);

        List<DataPoint> points = [.. orderedPoints.SkipWhile(x => x.X < TrendDefaults.FixedPeriodStartYear)];

        if (points.Count < TrendDefaults.MinimumYearsForTrend
            || points[0].X >= TrendDefaults.FixedPeriodStartYear + TrendDefaults.FixedPeriodStartToleranceYears)
        {
            return null;
        }

        return points;
    }
}
