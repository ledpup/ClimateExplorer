namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The effective radiative forcing by agent now, as horizontal bars: warming to the right of zero and cooling to the
/// left, then the total, and the non-radiative heat below a divider (energy balance plan §7, view 1). A visually
/// hidden table of the bars is its text alternative. It redraws whenever its values change.
/// </summary>
public partial class ForcingChart : IAsyncDisposable
{
    // The engines are the only non-radiative heat built so far
    private const string NonRadiativeHeatLabel = "Engines' heat (not a forcing)";

    private ElementReference canvas;
    private IJSObjectReference? module;
    private IReadOnlyList<double>? drawn;

    [Parameter]
    [EditorRequired]
    public ForcingBreakdown Forcing { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    // The agents, the total, then the non-radiative heat: the module draws the last two as the total and the divider
    private IReadOnlyList<Bar> Bars =>
    [
        .. Forcing.Agents.Select(forcing => new Bar(forcing.Agent.Name, forcing.Value)),
        new Bar("Total", Forcing.Total),
        new Bar(NonRadiativeHeatLabel, Forcing.NonRadiativeHeat),
    ];

    private string UnitText => Units.ShortLabel(UnitOfMeasure.WattsPerSquareMetre);

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            await module.InvokeVoidIfConnectedAsync("destroy", canvas);
            await module.DisposeAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (JsRuntime is null)
        {
            return;
        }

        var bars = Bars;
        var values = bars.Select(bar => bar.Value).ToList();

        if (firstRender)
        {
            module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./CarbonSim/Components/ForcingChart.razor.js");
            await module.InvokeVoidAsync("create", canvas, bars.Select(bar => bar.Label).ToList(), values, $"Forcing ({UnitText})", UnitText);
        }
        else if (module is not null && (drawn is null || !drawn.SequenceEqual(values)))
        {
            await module.InvokeVoidAsync("update", canvas, bars.Select(bar => bar.Label).ToList(), values);
        }

        drawn = values;
    }

    private string ValueText(double wattsPerSquareMetre) =>
        Units.Format(Measure.Level(wattsPerSquareMetre, UnitOfMeasure.WattsPerSquareMetre), signed: true, extraDecimals: 2);

    private sealed record Bar(string Label, double Value);
}
