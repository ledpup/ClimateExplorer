namespace ClimateExplorer.Web.Client.UiModel;

public sealed record RecentObservationStatViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
