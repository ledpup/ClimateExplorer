namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationTrendViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int CompleteYearCount { get; init; }
    public int MinimumRequiredYears { get; init; }
    public string? HeadlineText { get; init; }
    public string? HeadlineCaption { get; init; }
    public string? RecentTrendText { get; init; }
    public string? FirstHalfTrendText { get; init; }
    public string? UnavailableReason { get; init; }
}
