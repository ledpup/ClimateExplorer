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

    /// <summary>
    /// The first year of the Since 1961 window: the start of the WMO 1961-1990 reference period. It's
    /// the same period for every series, so its trend doesn't depend on how long the record is.
    /// </summary>
    public const int FixedPeriodStartYear = 1961;

    /// <summary>
    /// The Since 1961 window is only fitted when its first complete year falls within this many years of
    /// <see cref="FixedPeriodStartYear"/> (1961-1965). Without it, a series starting in 1990 would get a
    /// "since 1961" trend that is really its full period.
    /// </summary>
    public const int FixedPeriodStartToleranceYears = 5;
}
