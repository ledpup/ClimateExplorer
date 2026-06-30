namespace ClimateExplorer.Web.Client.Components.Common.Control;

using Microsoft.AspNetCore.Components;

public partial class ClimateDropdownButton
{
    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? MenuContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => string.IsNullOrWhiteSpace(Class)
        ? "climate-dropdown-toggle"
        : $"climate-dropdown-toggle {Class}";

    private string? EffectiveAriaLabel => AriaLabel ?? Text;
}
