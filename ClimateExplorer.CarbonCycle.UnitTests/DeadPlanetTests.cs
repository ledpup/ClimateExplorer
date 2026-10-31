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
        Assert.AreEqual(0, planet.CarbonDioxideForcing(state), 1e-12);
        Assert.IsTrue(state.FlameOn);
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
        Assert.AreEqual(state.WaterMade, state.LiquidWater, tolerance);
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
    }

    [TestMethod]
    public void Advance_HundredYears_CarbonDioxideWarmsFarMoreThanEngineHeat()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default);
        var start = planet.Start();

        var state = planet.Advance(start, 100 * Year);

        Assert.AreEqual(0.077, planet.EngineHeat(state), 0.005);
        Assert.AreEqual(6.6, planet.CarbonDioxideForcing(state), 0.2);
        Assert.AreEqual(2.1, state.Temperature - start.Temperature, 0.1);
    }

    [TestMethod]
    public void Advance_LongAfterEnginesStop_SettlesWhereCarbonDioxideBalancesHeatToSpace()
    {
        var settings = DeadPlanetSettings.Default;
        var planet = new DeadPlanet(settings);

        var state = planet.Advance(planet.Start(), 5000 * Year);

        Assert.AreEqual(0, planet.EngineHeat(state));
        Assert.AreEqual(settings.StartTemperature + (planet.CarbonDioxideForcing(state) / settings.ClimateFeedback), state.Temperature, 1e-6);
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
    public void Advance_NoHeatToSpace_EngineHeatAloneWarmsTheAir()
    {
        var planet = new DeadPlanet(DeadPlanetSettings.Default with { ClimateFeedback = 0 });
        var start = planet.Start();

        var state = planet.Advance(start, 1e5);

        // Too soon for the CO₂ to matter: 3.9e13 W into 5.2e21 J/K of air
        Assert.AreEqual(7.6e-4, state.Temperature - start.Temperature, 0.2e-4);
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

    private static double Carbon(SimulationState state) => state.CarbonDioxide + state.Methane;

    private static double Hydrogen(SimulationState state) => (4 * state.Methane) + (2 * (state.WaterVapour + state.LiquidWater));

    private static double Oxygen(SimulationState state) => (2 * state.Oxygen) + (2 * state.CarbonDioxide) + state.WaterVapour + state.LiquidWater;
}
