namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

/// <summary>
/// One end of a metric's historical range for the comparison date — the record high
/// or the record low — shown as reference context (value + year). The current
/// value's single rank lives on <see cref="RecentObservationRecordsViewModel"/>, not
/// here, so the two record ends are not themselves ranked.
/// </summary>
public sealed record RecentObservationMetricRecordViewModel
{
    public string Label { get; init; } = string.Empty; // "Record high" | "Record low"
    public string Value { get; init; } = string.Empty; // formatted record value
    public string? Year { get; init; }
}
