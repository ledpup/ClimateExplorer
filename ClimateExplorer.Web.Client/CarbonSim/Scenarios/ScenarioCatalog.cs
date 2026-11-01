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
            HouseFor,
            new ScenarioInputs { FossilFuels = new FossilFuelInputs { MethaneBurnerRate = 0.1 } }),

        new(
            "A billion cars on a dead planet",
            typeof(DeadPlanetScenario),
            DeadPlanetFor,
            new ScenarioInputs { FossilFuels = new FossilFuelInputs { Cars = 1e9 }, Planet = new PlanetInputs() }),

        // A placeholder, so there's something to cycle through until more scenarios are written
        new("Scenario 3", typeof(BlankScenario), HouseFor, ScenarioInputs.None),
    ];

    // Inputs without fossil fuels leave the burner off
    private static SealedHouse HouseFor(ScenarioInputs inputs) =>
        new(SealedHouseSettings.Default with { BurnerRate = inputs.FossilFuels?.MethaneBurnerRate ?? 0 });

    // Inputs without fossil fuels leave the cars parked; without a planet, the dry planet's sunlight and albedo
    private static DeadPlanet DeadPlanetFor(ScenarioInputs inputs)
    {
        var energy = PlanetEnergySettings.DryPlanet;

        if (inputs.Planet is not null)
        {
            energy = energy with { SolarConstant = inputs.Planet.SolarConstant, Albedo = inputs.Planet.Albedo };
        }

        return new(DeadPlanetSettings.Default with { Cars = inputs.FossilFuels?.Cars ?? 0, Energy = energy });
    }
}
