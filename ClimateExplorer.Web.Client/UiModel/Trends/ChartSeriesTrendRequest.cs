namespace ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// One entry in <see cref="ChartSeriesDefinition.Trends"/> - the user's request for a single trend
/// projection on a chart series (up to three per series). Mutable, like the definition it sits on:
/// this is user intent, edited in place by the UI, not derived state.
/// </summary>
/// <remarks>
/// <see cref="TrendPeriod"/> is only ever set to a window that came back statistically significant -
/// the data builder resolves it, since only it knows what was significant for the data actually
/// loaded. See <see cref="ChartSeriesTrend"/> for the computed result this request produces.
/// </remarks>
public sealed class ChartSeriesTrendRequest
{
    public TrendRegressionType RegressionType { get; set; } = TrendRegressionType.Linear;

    public TrendWindow? TrendPeriod { get; set; }

    public int TrendPredictionYears { get; set; } = TrendPredictionRange.Default;

    /// <summary>
    /// When set, projects the trend forward to this fixed calendar year instead of
    /// <see cref="TrendPredictionYears"/> years past the end of the record. Unlike
    /// <see cref="TrendPredictionYears"/> - a duration that keeps the projection's endpoint
    /// drifting forward as the underlying dataset grows - this stays pinned at the same year
    /// (e.g. a preset that always projects "to 2100"), recomputed against whatever the record's
    /// last year happens to be on each build. Editing "Predict until" in the UI clears this and
    /// falls back to the duration-based field, since interactive edits are anchored to "now", not
    /// to a fixed target.
    /// </summary>
    public int? TrendPredictionTargetYear { get; set; }

    /// <summary>A value-copy, independent of this instance - used wherever a series is duplicated or copied to a new location.</summary>
    public ChartSeriesTrendRequest Clone()
    {
        return new ChartSeriesTrendRequest
        {
            RegressionType = RegressionType,
            TrendPeriod = TrendPeriod,
            TrendPredictionYears = TrendPredictionYears,
            TrendPredictionTargetYear = TrendPredictionTargetYear,
        };
    }
}
