namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

using ClimateExplorer.CarbonCycle.Simulation;

/// <summary>
/// A planet setting's own inputs: its sunlight and albedo, as <see cref="PlanetEnergySettings"/>. Neither has a control on
/// screen yet; a scenario sets them.
/// </summary>
public sealed record PlanetInputs
{
    // W/m², as PlanetEnergySettings.SolarConstant
    public double SolarConstant { get; init; } = PlanetEnergySettings.DryPlanet.SolarConstant;

    // As PlanetEnergySettings.Albedo
    public double Albedo { get; init; } = PlanetEnergySettings.DryPlanet.Albedo;
}
