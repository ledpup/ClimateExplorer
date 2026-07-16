namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationTrendViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int CompleteYearCount { get; init; }
    public int MinimumRequiredYears { get; init; }
    public string? HeadlineText { get; init; }
    public bool IsHeadlinePositive { get; init; }
    public string? HeadlineCaption { get; init; }
    public string? RecentTrendYearRange { get; init; }
    public string? RecentTrendValueText { get; init; }
    public bool IsRecentTrendPositive { get; init; }
    public string? FirstHalfTrendYearRange { get; init; }
    public string? FirstHalfTrendValueText { get; init; }
    public bool IsFirstHalfTrendPositive { get; init; }
    public string? UnavailableReason { get; init; }
}
