namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationMainPanel
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

    private string ExpandLabel => Expansion.IsExpanded ? "Hide statistics" : "Show statistics";

    private async Task ToggleExpanded()
    {
        Expansion.Toggle();
        await OnExpansionChanged.InvokeAsync();
    }
}
