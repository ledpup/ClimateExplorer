namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationTile
{
    [Parameter]
    [EditorRequired]
    public RecentObservationTileViewModel Tile { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public RecentObservationTileExpansionState Expansion { get; set; } = default!;

    [Parameter]
    public bool IsRemovable { get; set; }

    [Parameter]
    public string RemoveButtonLabel { get; set; } = "Remove tile";

    [Parameter]
    public EventCallback OnRemove { get; set; }

    [Parameter]
    public EventCallback OnExpansionChanged { get; set; }

    [Parameter]
    public EventCallback<MetricGroupKey?> OnGroupSelected { get; set; }

    [Parameter]
    public EventCallback<TrendDownloadRequest> OnTrendDownloadRequested { get; set; }

    private string RemovableClass => IsRemovable ? "removable" : string.Empty;

    private string ExpandableClass => Tile.AvailableExpandedTabs.Count > 0 ? "expandable" : string.Empty;

    private string ExpandLabel => Expansion.IsExpanded ? "Hide statistics" : "Show statistics";

    private RecentObservationExpandedTabViewModel? SelectedTab =>
        Tile.AvailableExpandedTabs.FirstOrDefault(tab => Expansion.IsGroupSelected(tab.Key))
        ?? (Tile.AvailableExpandedTabs.Count > 0 ? Tile.AvailableExpandedTabs[0] : null);

    private bool IsDayRecordsSelected => SelectedTab?.Key == MetricGroupKey.DayRecords;

    private string ToneClass => Tile.Tone switch
    {
        RecentObservationTileTone.TemperatureWarm => "temperature-warm",
        RecentObservationTileTone.TemperatureCool => "temperature-cool",
        RecentObservationTileTone.PrecipitationWet => "precipitation-wet",
        RecentObservationTileTone.PrecipitationDry => "precipitation-dry",
        RecentObservationTileTone.Unavailable => "unavailable",
        _ => "neutral",
    };

    protected override void OnParametersSet()
    {
        Expansion.EnsureSelection(Tile.AvailableExpandedTabs);
    }

    private async Task ToggleExpanded()
    {
        Expansion.Toggle();
        await OnExpansionChanged.InvokeAsync();
    }

    private async Task SelectGroup(MetricGroupKey? key)
    {
        Expansion.SelectGroup(key);
        await OnGroupSelected.InvokeAsync(key);
    }
}
