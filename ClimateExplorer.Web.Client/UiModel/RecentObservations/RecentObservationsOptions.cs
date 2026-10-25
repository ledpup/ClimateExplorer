namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using ClimateExplorer.Web.Client.Units;

public sealed record RecentObservationsOptions
{
    public const int DefaultMinimumRankSampleSize = 10;

    public DateOnly? ReferenceDate { get; init; }
    public ComparisonEndMode ComparisonEndMode { get; init; } = ComparisonEndMode.FullDataset;
    public float CompletenessThreshold { get; init; } = RecentObservationCompletenessThreshold.Default;
    public int MinimumRankSampleSize { get; init; } = DefaultMinimumRankSampleSize;
    public int PreviousDayCount { get; init; } = int.MaxValue;
    public int PreviousMonthCount { get; init; } = int.MaxValue;
    public int PreviousSeasonCount { get; init; } = int.MaxValue;
    public int PreviousYearCount { get; init; } = int.MaxValue;

    /// <summary>
    /// The unit system the tiles' text is formatted in. Every comparison is still calculated in metric.
    /// </summary>
    public IUnitFormatter Units { get; init; } = UnitFormatter.Metric;
}
