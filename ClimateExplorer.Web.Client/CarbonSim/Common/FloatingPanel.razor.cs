namespace ClimateExplorer.Web.Client.CarbonSim.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// A raised panel that sits in the page's flow and can be dragged by its header, or moved with the arrow keys on its
/// handle. It moves with a CSS <c>translate</c>, so the space it left keeps the layout steady. On mobile it stays put.
/// </summary>
public partial class FloatingPanel : IAsyncDisposable
{
    private readonly string titleId = $"floating-panel-{Guid.NewGuid():N}";
    private ElementReference panel;
    private ElementReference header;
    private IJSObjectReference? module;

    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    private string HintId => $"{titleId}-hint";

    private string CssClass =>
        string.IsNullOrWhiteSpace(Class)
            ? "floating-panel"
            : $"floating-panel {Class}";

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
            {
                await module.InvokeVoidAsync("dispose", panel);
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
            module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./CarbonSim/Common/FloatingPanel.razor.js");
            await module.InvokeVoidAsync("init", panel, header);
        }
    }
}
