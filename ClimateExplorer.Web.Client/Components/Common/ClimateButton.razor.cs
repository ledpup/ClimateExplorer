namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

public partial class ClimateButton
{
    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Compact { get; set; }

    [Parameter]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Solid;

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

    private string CssClass
    {
        get
        {
            var classes = Variant switch
            {
                ButtonVariant.Link => "climate-link-button",
                _ => Compact ? "climate-button climate-button--compact" : "climate-button",
            };

            if (IsIconOnly)
            {
                classes += " climate-button--icon-only";
            }

            return string.IsNullOrWhiteSpace(Class)
                ? classes
                : $"{classes} {Class}";
        }
    }

    private string? EffectiveAriaLabel => AriaLabel ?? Text;

    // No label to size against (just an icon glyph) - Font Awesome forces line-height: 1 on its
    // icons, which otherwise leaves this button shorter than its text-labelled siblings. See
    // ClimateButton.razor.css.
    private bool IsIconOnly => !string.IsNullOrWhiteSpace(Icon) && string.IsNullOrWhiteSpace(Text) && ChildContent is null;
}
