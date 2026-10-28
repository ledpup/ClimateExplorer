namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Shows <see cref="Text"/> in a tooltip when <see cref="ChildContent"/> is hovered or gets keyboard focus.
/// Renders only data-tooltip attributes; wwwroot/js/tooltip.js (loaded from App.razor) shows the tooltip.
/// </summary>
public partial class DelayedTooltip
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The tooltip content. May contain simple markup (p, br, b, table); tooltip.js drops anything else.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string? Text { get; set; }

    /// <summary>
    /// Inline style for the wrapper, e.g. "display: block" when it wraps block-level content.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Wraps the text at 15rem. Otherwise the tooltip grows to fit its content, up to the viewport width.
    /// </summary>
    [Parameter]
    public bool Multiline { get; set; }

    [Parameter]
    public TooltipPlacement Placement { get; set; } = TooltipPlacement.Top;

    // Top is tooltip.js's default, so it isn't written out.
    private string? PlacementValue => Placement switch
    {
        TooltipPlacement.Bottom => "bottom",
        TooltipPlacement.Start => "start",
        TooltipPlacement.End => "end",
        _ => null,
    };
}
