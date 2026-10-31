namespace ClimateExplorer.CarbonCycle.Simulation;

using ClimateExplorer.CarbonCycle.Chemistry;

// A setting the clock can run: a burner in an enclosure of air, stepped forward from time 0.
public interface ISimulation
{
    // For display; its first reactant is the fuel
    Reaction Reaction { get; }

    // Mole fraction of O₂ below which the burner goes out
    double OxygenCutOff { get; }

    SimulationState Start();

    // The state the given number of seconds later
    SimulationState Advance(SimulationState state, double seconds);
}
