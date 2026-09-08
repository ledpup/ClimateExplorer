namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Lays out a small "pill" button nested inside the right-hand edge of a primary button, e.g.
/// [ Add month [12] ]. Both buttons stay fully independent - separately clickable, separately
/// disabled, separately labelled - only the visual nesting is shared here.
///
/// This is a pure layout wrapper: it takes no Text/Icon/Disabled/OnClick etc. of its own.
/// The caller supplies both buttons (typically a <see cref="ClimateButton"/> each) via
/// <see cref="PrimaryContent"/> and <see cref="PillContent"/>, tagging each with the
/// "pill-button-primary" / "pill-button-pill" CSS class so this component's stylesheet can find
/// and position them.
/// </summary>
public partial class PillButton
{
    [Parameter]
    public RenderFragment? PrimaryContent { get; set; }

    [Parameter]
    public RenderFragment? PillContent { get; set; }

    [Parameter]
    public string? Class { get; set; }

    private string CssClass => string.IsNullOrWhiteSpace(Class)
        ? "pill-button"
        : $"pill-button {Class}";
}
