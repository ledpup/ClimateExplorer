namespace ClimateExplorer.Web.Client.Components.Chart;

using ClimateExplorer.Web.UiLogic;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// A minimal Chart.js canvas: just the calls <see cref="ChartView"/> makes. Chart.js itself is
/// loaded on first use by the collocated JS module, so pages without a chart don't download it.
/// </summary>
public partial class ChartJsChart : IAsyncDisposable
{
    private ElementReference canvas;
    private IJSObjectReference? module;
    private DotNetObjectReference<ChartJsChart>? dotNetRef;
    private Task? initialisation;

    [Parameter]
    public string AriaLabel { get; set; } = "Chart";

    [Parameter]
    public EventCallback<ChartJsClickEventArgs> Clicked { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    public Task ClearAsync() => InvokeModuleAsync("clear");

    /// <summary>
    /// Replaces the labels and datasets. Takes effect on the next <see cref="UpdateAsync"/>.
    /// </summary>
    public Task SetDataAsync(ChartJsType type, IReadOnlyList<string> labels, IReadOnlyList<ChartJsDataset> datasets)
        => InvokeModuleAsync("setData", type.ToString().ToLowerInvariant(), labels, datasets);

    /// <summary>
    /// Replaces the options object. Takes effect on the next <see cref="UpdateAsync"/>.
    /// </summary>
    public Task SetOptionsAsync(object options) => InvokeModuleAsync("setOptions", options);

    public Task UpdateAsync() => InvokeModuleAsync("update");

    public Task ResizeAsync() => InvokeModuleAsync("resize");

    [JSInvokable]
    public Task OnChartClicked(int datasetIndex, int index)
    {
        return Clicked.InvokeAsync(new ChartJsClickEventArgs(datasetIndex, index));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (initialisation is not null)
            {
                await initialisation;
            }

            if (module is not null)
            {
                await module.InvokeVoidAsync("destroy", canvas);
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        dotNetRef?.Dispose();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        return firstRender ? EnsureInitialisedAsync() : Task.CompletedTask;
    }

    // The parent can call in from its own OnAfterRenderAsync before this component's has created the
    // chart (in Blazor Server especially), so every call waits for the same initialisation.
    private Task EnsureInitialisedAsync() => initialisation ??= InitialiseAsync();

    private async Task InitialiseAsync()
    {
        module = await JsRuntime!.InvokeAsync<IJSObjectReference>("import", "./Components/Chart/ChartJsChart.razor.js");
        dotNetRef = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("create", canvas, "line", dotNetRef);
    }

    private async Task InvokeModuleAsync(string identifier, params object?[] args)
    {
        await EnsureInitialisedAsync();
        await module!.InvokeVoidAsync(identifier, [canvas, .. args]);
    }
}
