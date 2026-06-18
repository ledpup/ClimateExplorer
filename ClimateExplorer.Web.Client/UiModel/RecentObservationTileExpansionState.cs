namespace ClimateExplorer.Web.Client.UiModel;

/// <summary>
/// UI-only expansion state for a single Recent Observation tile. Collapsed by default.
/// </summary>
public sealed class RecentObservationTileExpansionState
{
    public bool IsExpanded { get; private set; }

    public string? SelectedGroupKey { get; private set; }

    public void Expand()
    {
        IsExpanded = true;
    }

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
