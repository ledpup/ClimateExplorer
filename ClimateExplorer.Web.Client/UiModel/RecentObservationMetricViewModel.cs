namespace ClimateExplorer.Web.Client.UiModel;

using ClimateExplorer.Core.Calculators;

/// <summary>
/// A single statistic shown in an expanded tile: its current-period value and,
/// where available, the historical record it is compared against.
/// </summary>
public sealed record RecentObservationMetricViewModel
{
    public string Label { get; init; } = string.Empty;
    public string CurrentValue { get; init; } = string.Empty;
    public string? RecordValue { get; init; }
    public string? RecordYear { get; init; }
    public RecentObservationRecordStatus RecordStatus { get; init; }
    public string? RecordStatusText { get; init; }
    public string? RankText { get; init; }

    public bool HasRecord => !string.IsNullOrWhiteSpace(RecordValue);
}
