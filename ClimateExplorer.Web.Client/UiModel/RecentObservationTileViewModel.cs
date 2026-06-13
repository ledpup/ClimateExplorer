namespace ClimateExplorer.Web.Client.UiModel;

public sealed record RecentObservationTileViewModel
{
    public string PeriodTitle { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string PercentileSentence { get; init; } = string.Empty;
    public string PrimaryLabel { get; init; } = string.Empty;
    public string PrimaryValue { get; init; } = string.Empty;
    public RecentObservationTileTone Tone { get; init; }
    public bool HasComparison { get; init; }
    public string? Note { get; init; }
    public List<RecentObservationStatViewModel> Stats { get; init; } = [];
}
