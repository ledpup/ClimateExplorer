namespace ClimateExplorer.Web.Client.UiModel;

public sealed record LatestRecordsTabResult
{
    public bool IsSupported { get; init; } = true;
    public string EmptyMessage { get; init; } = "No latest records are available.";
    public List<LatestRecordTileViewModel> Tiles { get; init; } = [];
}
