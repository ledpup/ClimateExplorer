namespace ClimateExplorer.CarbonCycle.Simulation;

// One agent's effective radiative forcing, W/m²
public sealed record Forcing(ForcingAgent Agent, double Value);
