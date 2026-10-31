namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;

/// <summary>
/// The base of every scenario's slide in the carousel: its description, the controls for the inputs it uses, and
/// what the run has done so far. The page owns the inputs and the run; the active slide gets the live ones, and a
/// slide beside it the scenario's own inputs at time 0.
/// </summary>
public abstract class ScenarioComponent : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public Scenario Scenario { get; set; } = default!;

    /// <summary>The inputs in use: the scenario's own until the reader changes them.</summary>
    [Parameter]
    [EditorRequired]
    public ScenarioInputs Inputs { get; set; } = default!;

    /// <summary>Raised with the new inputs when the reader changes one.</summary>
    [Parameter]
    public EventCallback<ScenarioInputs> InputsChanged { get; set; }

    /// <summary>Raised when the reader types an input that can't be used, so the run can't play.</summary>
    [Parameter]
    public EventCallback InputInvalid { get; set; }

    [Parameter]
    [EditorRequired]
    public ISimulation Simulation { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public SimulationState State { get; set; } = default!;

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    protected Task OnFossilFuelsChangedAsync(FossilFuelInputs fossilFuels) =>
        InputsChanged.InvokeAsync(Inputs with { FossilFuels = fossilFuels });
}
