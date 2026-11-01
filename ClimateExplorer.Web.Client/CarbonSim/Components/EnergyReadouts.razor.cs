namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// A planet's energy budget now: its temperature, the sunlight it absorbs against the heat it sends to space, the
/// imbalance between them, the total forcing, and its warming per doubling of CO₂ (energy balance plan §7).
/// </summary>
public partial class EnergyReadouts
{
    [Parameter]
    [EditorRequired]
    public IPlanetSimulation Simulation { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public SimulationState State { get; set; } = default!;

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private EnergyBudget Budget => Simulation.Budget(State);

    private ForcingBreakdown Forcing => Simulation.Forcing(State);

    private string TemperatureText => Units.Format(Measure.Level(State.Temperature - 273.15, UnitOfMeasure.DegreesCelsius));

    private string PerDoublingText => Units.Format(Measure.Change(Simulation.WarmingPerDoubling, UnitOfMeasure.DegreesCelsius));

    // To 0.01 W/m², so the imbalance is seen to stay small while the forcing grows
    private string FluxText(double wattsPerSquareMetre, bool signed = false) =>
        Units.Format(Measure.Level(wattsPerSquareMetre, UnitOfMeasure.WattsPerSquareMetre), signed, extraDecimals: 1);
}
