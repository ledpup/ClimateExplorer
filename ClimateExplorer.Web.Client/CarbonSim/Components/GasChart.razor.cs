namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// A line per gas, as a percentage of the air, against simulated time. The parent resets it with the series and
/// appends a point per tick; Chart.js keeps the history.
/// </summary>
public partial class GasChart : IAsyncDisposable
{
    private ElementReference canvas;
    private IJSObjectReference? module;
    private Task? initialisation;

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<GasChartSeries> Series { get; set; } = [];

    [Parameter]
    public string AriaLabel { get; set; } = "Chart of the gases over time";

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    public async Task ResetAsync()
    {
        await EnsureInitialisedAsync();
        await module!.InvokeVoidAsync("reset", canvas, Series);
    }

    public async Task AppendAsync(double time, IReadOnlyList<double> values)
    {
        await EnsureInitialisedAsync();
        await module!.InvokeVoidAsync("append", canvas, time, values);
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
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        firstRender ? EnsureInitialisedAsync() : Task.CompletedTask;

    // The parent can call in from its own OnAfterRenderAsync before this one has created the chart
    private Task EnsureInitialisedAsync() => initialisation ??= InitialiseAsync();

    private async Task InitialiseAsync()
    {
        module = await JsRuntime!.InvokeAsync<IJSObjectReference>("import", "./CarbonSim/Components/GasChart.razor.js");
        await module.InvokeVoidAsync("create", canvas, Series, "Share of the air");
    }
}
