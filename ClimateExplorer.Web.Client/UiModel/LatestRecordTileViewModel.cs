namespace ClimateExplorer.Web.Client.UiModel;

public sealed record LatestRecordTileViewModel
{
    public string PeriodTitle { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string PercentileSentence { get; init; } = string.Empty;
    public string PrimaryLabel { get; init; } = string.Empty;
    public string PrimaryValue { get; init; } = string.Empty;
    public LatestRecordTileTone Tone { get; init; }
    public bool HasComparison { get; init; }
    public string? Note { get; init; }
    public List<LatestRecordStatViewModel> Stats { get; init; } = [];
}
