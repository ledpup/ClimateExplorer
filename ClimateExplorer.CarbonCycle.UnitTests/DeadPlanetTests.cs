using System.Linq;
using ClimateExplorer.CarbonCycle.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class DeadPlanetTests
{
    private const double Year = 365.25 * 24 * 3600;

    [TestMethod]
    public void Start_DefaultSettings_HoldsDryAirAtEarthsSurfacePressure()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        var state = planet.Start();

        Assert.AreEqual(98.7e3, state.Pressure, 0.1e3);
        Assert.AreEqual(DryAir.Oxygen, state.OxygenFraction, 1e-12);
        Assert.AreEqual(0, state.WaterVapour);
        Assert.AreEqual(0, planet.Forcing(state).Total, 1e-12);
        Assert.IsTrue(state.FlameOn);
    }

    [TestMethod]
    public void Start_DefaultSettings_InBalanceWithTheSunBelowFreezing()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        var state = planet.Start();

        // −12 °C: absorbed sunlight against dry air's small greenhouse effect
        Assert.AreEqual(261.4, state.Temperature, 0.1);
        Assert.AreEqual(0, planet.Budget(state).Imbalance, 1e-9);
        Assert.AreEqual(1.08, planet.WarmingPerDoubling, 0.01);
    }

    [TestMethod]
    public void Start_BrighterSun_WarmerStartWithNoForcing()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default with { Energy = PlanetEnergySettings.DryPlanet with { SolarConstant = 1400 } });

        var state = planet.Start();

        Assert.AreEqual(planet.EnergyBalance.StartTemperature, state.Temperature);
        Assert.IsGreaterThan(new DeadPlanet(DeadPlanetSettings.Default).Start().Temperature, state.Temperature);
        Assert.AreEqual(0, planet.Forcing(state).Of(ForcingAgents.Solar));
        Assert.AreEqual(0, planet.Budget(state).Imbalance, 1e-9);
    }

    [TestMethod]
    public void Constructor_BillionCars_BurnAboutSevenMillionMolesOfOctaneASecond()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        Assert.AreEqual(7.18e6, planet.FuelMolesPerSecond, 0.01e6);
    }

    [TestMethod]
    public void Advance_EnginesRunning_ConservesElements()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);
        var start = planet.Start();

        var state = planet.Advance(start, 10 * Year);

        // Octane, C₈H₁₈, comes in through the engines; nothing leaves.
        var tolerance = 1e-9 * start.GasMoles;
        Assert.AreEqual(Carbon(start) + (8 * state.FuelBurnt), Carbon(state), tolerance);
        Assert.AreEqual(Hydrogen(start) + (18 * state.FuelBurnt), Hydrogen(state), tolerance);
        Assert.AreEqual(Oxygen(start), Oxygen(state), tolerance);
        Assert.AreEqual(start.Nitrogen, state.Nitrogen);
        Assert.AreEqual(state.WaterMade, state.LiquidWater + state.Ice, tolerance);
    }

    [TestMethod]
    public void Advance_BelowFreezing_WaterFallsAsFrost()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        var state = planet.Advance(planet.Start(), 10 * Year);

        Assert.IsLessThan(273.15, state.Temperature);
        Assert.IsGreaterThan(0, state.Ice);
        Assert.AreEqual(state.WaterMade, state.Ice);
        Assert.AreEqual(0, state.LiquidWater);
    }

    [TestMethod]
    public void Advance_OneYear_CarbonDioxideRisesAboutTenPpm()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);
        var start = planet.Start();

        var state = planet.Advance(start, Year);

        var rise = (state.CarbonDioxide / state.GasMoles) - (start.CarbonDioxide / start.GasMoles);
        Assert.AreEqual(10.2e-6, rise, 0.2e-6);
    }

    [TestMethod]
    public void Advance_BillionCars_EnginesStopAfterAboutFourThousandYears()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        var state = planet.Advance(planet.Start(), 5000 * Year);

        Assert.IsFalse(state.FlameOn);
        Assert.AreEqual(3930 * Year, state.FlameOutTime!.Value, 50 * Year);

        var atCutOff = planet.Advance(planet.Start(), state.FlameOutTime.Value);
        Assert.AreEqual(DeadPlanetSettings.Default.OxygenCutOff, atCutOff.OxygenFraction, 1e-3);

        // About 4.2% CO₂, well past the forcing formula's fitted range (energy balance plan Q24)
        Assert.AreEqual(0.0416, atCutOff.CarbonDioxide / atCutOff.GasMoles, 0.0005);
        Assert.AreEqual(26.0, planet.Forcing(atCutOff).Of(ForcingAgents.CarbonDioxide), 0.2);
        Assert.AreEqual(268.2, atCutOff.Temperature, 0.2);
    }

    [TestMethod]
    public void Advance_HundredYears_CarbonDioxideWarmsFarMoreThanEngineHeat()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);
        var start = planet.Start();

        var state = planet.Advance(start, 100 * Year);

        var forcing = planet.Forcing(state);
        Assert.AreEqual(0.078, forcing.NonRadiativeHeat, 0.005);
        Assert.AreEqual(7.0, forcing.Of(ForcingAgents.CarbonDioxide), 0.2);
        Assert.AreEqual(1.9, state.Temperature - start.Temperature, 0.1);
    }

    [TestMethod]
    public void Forcing_EnginesRunning_EngineHeatBesideTheForcingsNotInTheirTotal()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        var forcing = planet.Forcing(planet.Advance(planet.Start(), 10 * Year));

        CollectionAssert.AreEqual(ForcingAgents.All.ToArray(), forcing.Agents.Select(x => x.Agent).ToArray());
        Assert.IsGreaterThan(0, forcing.NonRadiativeHeat);
        Assert.AreEqual(0, forcing.Of(ForcingAgents.Solar));
        Assert.AreEqual(0, forcing.Of(ForcingAgents.Albedo));
        Assert.AreEqual(forcing.Of(ForcingAgents.CarbonDioxide), forcing.Total);
    }

    [TestMethod]
    public void Advance_HundredYears_HeatToSpaceKeepsUpWithTheSunlight()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        var budget = planet.Budget(planet.Advance(planet.Start(), 100 * Year));

        // The air warms within weeks, so it lags the climbing forcing by only a few thousandths of a W/m²
        Assert.AreEqual(0.0035, budget.Imbalance + budget.NonRadiativeHeat, 0.001);
    }

    [TestMethod]
    public void Advance_LongAfterEnginesStop_SettlesWhereHeatToSpaceBalancesTheSunlight()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);

        var state = planet.Advance(planet.Start(), 5000 * Year);

        Assert.AreEqual(0, planet.Forcing(state).NonRadiativeHeat);
        Assert.AreEqual(0, planet.Budget(state).Imbalance, 1e-4);
    }

    [TestMethod]
    public void Advance_OneLargeStepOrManySmallOnes_SameResult()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);
        var large = planet.Advance(planet.Start(), 100 * Year);

        var small = planet.Start();
        for (var i = 0; i < 1000; i++)
        {
            small = planet.Advance(small, 0.1 * Year);
        }

        Assert.AreEqual(large.CarbonDioxide, small.CarbonDioxide, 1e-9 * large.CarbonDioxide);
        Assert.AreEqual(large.Temperature, small.Temperature, 0.02);
    }

    [TestMethod]
    public void Advance_NoCars_NothingChanges()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default with { Cars = 0 });
        var start = planet.Start();

        var state = planet.Advance(start, 1000 * Year);

        Assert.IsFalse(state.FlameOn);
        Assert.AreEqual(start.Temperature, state.Temperature, 1e-9);
        Assert.AreEqual(start.Oxygen, state.Oxygen);
        Assert.AreEqual(1000 * Year, state.Time, 1e-3);
    }

    [TestMethod]
    public void Constructor_NegativeCars_Throws()
    {
        Assert.ThrowsExactly<InputValidationException>(() => new DeadPlanet(DeadPlanetSettings.Default with { Cars = -1 }));
    }

    [TestMethod]
    public void Constructor_AlbedoOfOne_RejectsAlbedo()
    {
        Assert.AreEqual(
            "planet.albedo",
            TestAssert.ThrowsForField(() => new DeadPlanet(DeadPlanetSettings.Default with { Energy = PlanetEnergySettings.DryPlanet with { Albedo = 1 } })));
    }

    private static double Carbon(SimulationState state) => state.CarbonDioxide + state.Methane;

    private static double Hydrogen(SimulationState state) => (4 * state.Methane) + (2 * (state.WaterVapour + state.LiquidWater + state.Ice));

    private static double Oxygen(SimulationState state) => (2 * state.Oxygen) + (2 * state.CarbonDioxide) + state.WaterVapour + state.LiquidWater + state.Ice;
}
