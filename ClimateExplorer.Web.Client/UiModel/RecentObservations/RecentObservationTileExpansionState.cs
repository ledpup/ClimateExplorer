namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

/// <summary>
/// UI-only expansion state for a single Recent Observation tile. Collapsed by default.
/// </summary>
public sealed class RecentObservationTileExpansionState
{
    public bool IsExpanded { get; private set; }

    public MetricGroupKey? SelectedGroupKey { get; private set; }

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

    // <summary>
    // Ensures a sensible selected tab once tabs are known. Defaults to the
    // first tab (e.g. "Period records") and only fills a missing/stale selection.
    // </summary>
    public void EnsureSelection(IReadOnlyList<RecentObservationExpandedTabViewModel> tabs)
    {
        if (tabs.Count == 0)
        {
            SelectedGroupKey = null;
            return;
        }

        if (SelectedGroupKey is null || tabs.All(x => x.Key != SelectedGroupKey))
        {
            SelectedGroupKey = tabs[0].Key;
        }
    }

    public void SelectGroup(MetricGroupKey? key)
    {
        SelectedGroupKey = key;
    }

    public bool IsGroupSelected(MetricGroupKey? group)
    {
        return SelectedGroupKey == group;
    }
}
