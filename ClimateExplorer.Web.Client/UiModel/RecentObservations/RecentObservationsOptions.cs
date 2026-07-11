namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationsOptions
{
    public const int DefaultMinimumRankSampleSize = 10;

    public DateOnly? ReferenceDate { get; init; }
    public ComparisonEndMode ComparisonEndMode { get; init; } = ComparisonEndMode.FullDataset;
    public float CompletenessThreshold { get; init; } = RecentObservationCompletenessThreshold.Default;
    public int MinimumRankSampleSize { get; init; } = DefaultMinimumRankSampleSize;
    public int PreviousDayCount { get; init; } = RecentObservationPeriodSelection.MaximumPreviousDayCount;
    public int PreviousMonthCount { get; init; } = RecentObservationPeriodSelection.MaximumPreviousMonthCount;
    public int PreviousSeasonCount { get; init; } = RecentObservationPeriodSelection.MaximumPreviousSeasonCount;
    public int PreviousYearCount { get; init; }
}
