namespace ClimateExplorer.Web.Client.UiModel;

public sealed record RecentObservationTileViewModel
{
    public RecentObservationPeriodKind PeriodKind { get; init; }
    public int? PeriodOffset { get; init; }
    public DateOnly PeriodStartDate { get; init; }
    public DateOnly PeriodEndDate { get; init; }
    public string PeriodTitle { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string PercentileSentence { get; init; } = string.Empty;
    public string PrimaryLabel { get; init; } = string.Empty;
    public string PrimaryValue { get; init; } = string.Empty;
    public string? HistoricalMaxLabel { get; init; }
    public string? HistoricalMaxValue { get; init; }
    public string? HistoricalMaxOccurred { get; init; }
    public string? HistoricalMinLabel { get; init; }
    public string? HistoricalMinValue { get; init; }
    public string? HistoricalMinOccurred { get; init; }
    public RecentObservationTileTone Tone { get; init; }
    public bool HasComparison { get; init; }
    public string? Note { get; init; }
    public List<RecentObservationStatViewModel> Stats { get; init; } = [];
}
