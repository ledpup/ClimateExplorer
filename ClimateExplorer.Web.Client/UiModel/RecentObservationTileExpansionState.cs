namespace ClimateExplorer.Web.Client.UiModel;

/// <summary>
/// UI-only expansion state for a single Recent Observation tile. Held by the tile
/// component instance so it survives view-model re-creation (e.g. completeness
/// threshold changes) without reloading data. Collapsed by default.
/// </summary>
public sealed class RecentObservationTileExpansionState
{
    public bool IsExpanded { get; private set; }

    public string? SelectedGroupKey { get; private set; }

    public void Toggle()
    {
        IsExpanded = !IsExpanded;
    }

    public void Collapse()
    {
        IsExpanded = false;
    }

    /// <summary>
    /// Ensures a sensible selected group once groups are known. Defaults to the
    /// first group (e.g. "Period") and only fills a missing/stale selection.
    /// </summary>
    public void EnsureSelection(IReadOnlyList<RecentObservationMetricGroupViewModel> groups)
    {
        if (groups.Count == 0)
        {
            SelectedGroupKey = null;
            return;
        }

        if (SelectedGroupKey is null || groups.All(x => x.Key != SelectedGroupKey))
        {
            SelectedGroupKey = groups[0].Key;
        }
    }

    public void SelectGroup(string key)
    {
        SelectedGroupKey = key;
    }

    public bool IsGroupSelected(string key)
    {
        return SelectedGroupKey == key;
    }
}
