namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed class RecentObservationTileExpansionStateCollection
{
    private readonly Dictionary<string, RecentObservationTileExpansionState> states = [];

    public RecentObservationTileExpansionState GetOrAdd(string tileKey)
    {
        if (!states.TryGetValue(tileKey, out var state))
        {
            state = new RecentObservationTileExpansionState();
            states[tileKey] = state;
        }

        return state;
    }

    public string CreateToggleAllLabel(IEnumerable<RecentObservationTileExpansionTarget> tiles)
    {
        return HasCollapsedExpandableTile(tiles) ? "Expand all" : "Collapse all";
    }

    public bool HasExpandableTile(IEnumerable<RecentObservationTileExpansionTarget> tiles)
    {
        return tiles.Any(tile => tile.IsExpandable);
    }

    public bool AreAllExpandableTilesExpanded(IEnumerable<RecentObservationTileExpansionTarget> tiles)
    {
        var expandableTiles = tiles.Where(tile => tile.IsExpandable).ToList();
        return expandableTiles.Count > 0 && expandableTiles.All(tile => GetOrAdd(tile.Key).IsExpanded);
    }

    public void ToggleAll(IEnumerable<RecentObservationTileExpansionTarget> tiles)
    {
        var tileList = tiles.ToList();
        if (HasCollapsedExpandableTile(tileList))
        {
            ExpandAll(tileList);
            return;
        }

        CollapseAll(tileList);
    }

    public void ExpandAll(IEnumerable<RecentObservationTileExpansionTarget> tiles)
    {
        foreach (var tile in tiles.Where(tile => tile.IsExpandable))
        {
            GetOrAdd(tile.Key).Expand();
        }
    }

    public void CollapseAll(IEnumerable<RecentObservationTileExpansionTarget> tiles)
    {
        foreach (var tile in tiles.Where(tile => tile.IsExpandable))
        {
            GetOrAdd(tile.Key).Collapse();
        }
    }

    private bool HasCollapsedExpandableTile(IEnumerable<RecentObservationTileExpansionTarget> tiles)
    {
        return tiles.Any(tile => tile.IsExpandable && !GetOrAdd(tile.Key).IsExpanded);
    }
}
