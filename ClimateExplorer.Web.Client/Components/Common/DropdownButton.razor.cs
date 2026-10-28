namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// A button that shows and hides a menu (disclosure pattern). The menu is a <c>popover="auto"</c>, so the browser
/// opens and closes it, closes it on Escape or a click outside, and renders it in the top layer;
/// DropdownButton.razor.js positions it below the button. Put <see cref="DropdownItem"/>s, checkboxes or links in
/// <see cref="MenuContent"/>.
/// </summary>
public partial class DropdownButton : IAsyncDisposable
{
    private readonly string menuId = $"climate-dropdown-{Guid.NewGuid():N}";
    private ElementReference toggle;
    private ElementReference menu;
    private IJSObjectReference? module;
    private IJSObjectReference? dropdown;

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Lines the menu up with the button's end edge instead of its start edge. For buttons at the end of a bar.
    /// </summary>
    [Parameter]
    public bool AlignEnd { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? MenuContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    internal string MenuId => menuId;

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    private string CssClass => string.IsNullOrWhiteSpace(Class)
        ? "climate-dropdown-toggle"
        : $"climate-dropdown-toggle {Class}";

    private string? EffectiveAriaLabel => AriaLabel ?? Text;

    // While prerendered the button is disabled, so the menu can't open unpositioned with items that do nothing
    private bool IsInteractive => RendererInfo.IsInteractive;

    public async Task CloseAsync()
    {
        try
        {
            if (dropdown is not null)
            {
                await dropdown.InvokeVoidAsync("hide");
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (dropdown is not null)
            {
                await dropdown.InvokeVoidAsync("dispose");
                await dropdown.DisposeAsync();
            }

            if (module is not null)
            {
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && JsRuntime is not null)
        {
            module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Common/DropdownButton.razor.js");
            dropdown = await module.InvokeAsync<IJSObjectReference>("init", toggle, menu);
        }
    }
}
