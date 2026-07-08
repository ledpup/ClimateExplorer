namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationVariationViewModel
{
    public string Label { get; init; } = string.Empty;
    public double? HistoricalMinimum { get; init; }
    public double? HistoricalMaximum { get; init; }
    public double? TypicalVariation { get; init; }
    public double? VariationScore { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string? HistoricalRangeText { get; init; }
    public string? TypicalVariationText { get; init; }
    public string? VariationScoreText { get; init; }
    public string? UnavailableReason { get; init; }
    public int ComparablePeriodCount { get; init; }
}
