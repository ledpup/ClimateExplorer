namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

using ClimateExplorer.CarbonCycle.Simulation;

/// <summary>
/// A preset scenario (simulation plan §2.4): its setting, and the inputs it starts with. The reader can change the
/// inputs before pressing play.
/// </summary>
/// <param name="Title">The scenario's heading, and its name on the carousel's arrows and dots.</param>
/// <param name="Component">The <see cref="ScenarioComponent"/> that shows the scenario as a slide in the carousel.</param>
/// <param name="Setting">The sealed house the scenario runs in. Its burner rate comes from the fossil fuel inputs.</param>
/// <param name="Inputs">The inputs the scenario starts with.</param>
public sealed record Scenario(string Title, Type Component, SealedHouseSettings Setting, ScenarioInputs Inputs);
