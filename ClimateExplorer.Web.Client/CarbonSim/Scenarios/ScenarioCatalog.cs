namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

using ClimateExplorer.CarbonCycle.Simulation;

/// <summary>The scenarios in the carousel, in order. The first is shown when the page opens.</summary>
public static class ScenarioCatalog
{
    public static IReadOnlyList<Scenario> All { get; } =
    [
        new(
            "Methane burning in a sealed house",
            typeof(SealedHouseScenario),
            SealedHouseSettings.Default,
            new ScenarioInputs { FossilFuels = new FossilFuelInputs { MethaneBurnerRate = 0.1 } }),

        // Placeholders, so there's something to cycle through until more scenarios are written
        new("Scenario 2", typeof(BlankScenario), SealedHouseSettings.Default, ScenarioInputs.None),
        new("Scenario 3", typeof(BlankScenario), SealedHouseSettings.Default, ScenarioInputs.None),
    ];
}
