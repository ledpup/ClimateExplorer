namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

public partial class ClimateLinkButton
{
    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string Type { get; set; } = "button";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => string.IsNullOrWhiteSpace(Class)
        ? "climate-link-button"
        : $"climate-link-button {Class}";

    private string? EffectiveAriaLabel => AriaLabel ?? Text;
}
