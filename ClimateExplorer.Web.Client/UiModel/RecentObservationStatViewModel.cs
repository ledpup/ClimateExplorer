namespace ClimateExplorer.Web.Client.UiModel;

using ClimateExplorer.Core.Calculators;

public sealed record RecentObservationStatViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public RecentObservationRecordStatus RecordStatus { get; init; }
    public string? RecordStatusText { get; init; }
}
