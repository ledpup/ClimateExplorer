namespace ClimateExplorer.CarbonCycle.Simulation;

// A planet setting: one temperature, warmed by the sun, with its forcing reported by agent (energy balance plan §3)
public interface IPlanetSimulation : ISimulation
{
    PlanetEnergySettings Energy { get; }

    // K per doubling of CO₂, at the start
    double WarmingPerDoubling { get; }

    ForcingBreakdown Forcing(SimulationState state);

    EnergyBudget Budget(SimulationState state);
}
