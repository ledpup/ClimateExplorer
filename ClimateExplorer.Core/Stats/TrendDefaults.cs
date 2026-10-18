namespace ClimateExplorer.Core.Stats;

using ClimateExplorer.Core.Calculators;

/// <summary>
/// Trend thresholds shared by the chart's trend module and the location dashboard, so the two never
/// disagree about whether a series has a trend. Recent Observations keeps its own, stricter 60-year
/// minimum.
/// </summary>
public static class TrendDefaults
{
    /// <summary>
    /// The WMO standard-normal length: below this many complete years, no trend is fitted at all.
    /// </summary>
    public const int MinimumYearsForTrend = 30;

    public const int RecentWindowYears = 30;

    /// <summary>
    /// The last-30-years and early-period windows are only fitted from this many complete years - the
    /// same rule as the warming anomaly, which compares exactly those two sub-periods. At 60 years each
    /// sub-period has 30 points of its own, and the last 30 is genuinely different from the full period.
    /// </summary>
    public const int MinimumYearsForSubPeriodWindows = AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly;
}
