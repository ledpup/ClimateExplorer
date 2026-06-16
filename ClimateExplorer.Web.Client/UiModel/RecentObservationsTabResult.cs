namespace ClimateExplorer.Web.Client.UiModel;

public sealed record RecentObservationsTabResult
{
    public bool IsSupported { get; init; } = true;
    public string EmptyMessage { get; init; } = "No recent observations are available.";
    public List<RecentObservationTileViewModel> Tiles { get; init; } = [];

    public RecentObservationsTabResult ApplyCompletenessThreshold(float completenessThreshold)
    {
        return this with
        {
            Tiles = [.. Tiles.Select(tile => tile.ApplyCompletenessThreshold(completenessThreshold))],
        };
    }
}
