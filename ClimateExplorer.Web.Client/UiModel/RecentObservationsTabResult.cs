namespace ClimateExplorer.Web.Client.UiModel;

public sealed record RecentObservationsTabResult
{
    public bool IsSupported { get; init; } = true;
    public string EmptyMessage { get; init; } = "No recent observations are available.";
    public List<RecentObservationTileViewModel> Tiles { get; init; } = [];
}
