namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// An action in a <see cref="DropdownButton"/>'s menu. Clicking it closes the menu (natively, through
/// <c>popovertargetaction="hide"</c>) and raises <see cref="OnClick"/>.
/// </summary>
public partial class DropdownItem
{
    [CascadingParameter]
    public DropdownButton? Parent { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => string.IsNullOrWhiteSpace(Class)
        ? "dropdown-item"
        : $"dropdown-item {Class}";
}
