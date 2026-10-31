using ClimateExplorer.CarbonCycle.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class SealedHouseTests
{
    private const double Hour = 3600;

    [TestMethod]
    public void Start_DefaultSettings_HoldsDryAirAtTargetPressure()
    {
        var house = new SealedHouse(SealedHouseSettings.Default);

        var state = house.Start();
        var dry = state.GasMoles - state.WaterVapour;

        Assert.AreEqual(SealedHouseSettings.Default.TargetPressure, state.Pressure, 1e-6);
        Assert.AreEqual(DryAir.Oxygen, state.Oxygen / dry, 1e-12);
        Assert.AreEqual(DryAir.Argon, state.Argon / dry, 1e-12);
        Assert.IsTrue(state.FlameOn);
    }

    [TestMethod]
    public void Advance_FlameBurning_ConservesElements()
    {
        var house = new SealedHouse(SealedHouseSettings.Default);
        var start = house.Start();

        var state = house.Advance(start, 120);

        // Methane comes in through the burner; N₂ goes out through the vent.
        Assert.AreEqual(Carbon(start) + state.FuelBurnt, Carbon(state), 1e-6);
        Assert.AreEqual(Hydrogen(start) + (4 * state.FuelBurnt), Hydrogen(state), 1e-6);
        Assert.AreEqual(Oxygen(start), Oxygen(state), 1e-6);
        Assert.AreEqual(start.Nitrogen, state.Nitrogen + state.NitrogenVented, 1e-6);
        Assert.AreEqual(start.Argon, state.Argon, 1e-9);
    }

    [TestMethod]
    public void Advance_FlameBurning_ProductsMatchMethaneEquation()
    {
        var house = new SealedHouse(SealedHouseSettings.Default);

        var state = house.Advance(house.Start(), 30);

        Assert.AreEqual(2 * state.FuelBurnt, state.OxygenUsed, 1e-9);
        Assert.AreEqual(state.FuelBurnt, state.CarbonDioxideMade, 1e-9);
        Assert.AreEqual(2 * state.FuelBurnt, state.WaterMade, 1e-9);
        Assert.AreEqual(802e3 * state.FuelBurnt, state.HeatReleased, 2e3 * state.FuelBurnt);
    }

    [TestMethod]
    public void Advance_HundredLitresPerSecond_FlameGoesOutInAboutAMinute()
    {
        var house = new SealedHouse(SealedHouseSettings.Default);

        var state = house.Advance(house.Start(), 600);

        Assert.IsFalse(state.FlameOn);
        Assert.AreEqual(70, state.FlameOutTime!.Value, 15);

        var atCutOff = house.Advance(house.Start(), state.FlameOutTime.Value);
        Assert.AreEqual(SealedHouseSettings.Default.OxygenCutOff, atCutOff.OxygenFraction, 1e-3);
    }

    [TestMethod]
    public void Advance_GasHob_FlameGoesOutInAboutADay()
    {
        var house = new SealedHouse(SealedHouseSettings.Default with { BurnerRate = 0.07e-3 });

        var state = house.Advance(house.Start(), 100 * Hour);

        Assert.IsFalse(state.FlameOn);
        Assert.AreEqual(28 * Hour, state.FlameOutTime!.Value, 6 * Hour);
    }

    [TestMethod]
    public void Advance_OneLargeStepOrManySmallOnes_SameFlameOutTime()
    {
        var house = new SealedHouse(SealedHouseSettings.Default);
        var large = house.Advance(house.Start(), 200);

        var small = house.Start();
        for (var i = 0; i < 2000; i++)
        {
            small = house.Advance(small, 0.1);
        }

        Assert.AreEqual(large.FlameOutTime!.Value, small.FlameOutTime!.Value, 0.5);
        Assert.AreEqual(large.Temperature, small.Temperature, 0.5);
    }

    [TestMethod]
    public void Advance_FlameBurning_WarmsHouseAndNeverExceedsTargetPressure()
    {
        var house = new SealedHouse(SealedHouseSettings.Default);
        var state = house.Start();

        for (var i = 0; i < 100; i++)
        {
            state = house.Advance(state, 1);
            Assert.IsLessThanOrEqualTo(SealedHouseSettings.Default.TargetPressure * (1 + 1e-9), state.Pressure);
        }

        Assert.IsGreaterThan(SealedHouseSettings.Default.StartTemperature + 5, state.Temperature);
    }

    [TestMethod]
    public void Advance_LongAfterFlameOut_CoolsToOutsideTemperature()
    {
        var house = new SealedHouse(SealedHouseSettings.Default);

        var state = house.Advance(house.Start(), 30 * 24 * Hour);

        Assert.AreEqual(SealedHouseSettings.Default.OutsideTemperature, state.Temperature, 0.1);
    }

    [TestMethod]
    public void Advance_NoBurner_NothingChanges()
    {
        var house = new SealedHouse(SealedHouseSettings.Default with { BurnerRate = 0 });
        var start = house.Start();

        var state = house.Advance(start, 24 * Hour);

        Assert.IsFalse(state.FlameOn);
        Assert.AreEqual(start.Temperature, state.Temperature, 1e-9);
        Assert.AreEqual(start.Oxygen, state.Oxygen, 1e-9);
        Assert.AreEqual(24 * Hour, state.Time, 1e-6);
    }

    [TestMethod]
    public void Constructor_NegativeVolume_Throws()
    {
        Assert.ThrowsExactly<InputValidationException>(() => new SealedHouse(SealedHouseSettings.Default with { Volume = -1 }));
    }

    private static double Carbon(SimulationState state) => state.CarbonDioxide + state.Methane;

    private static double Hydrogen(SimulationState state) => (4 * state.Methane) + (2 * (state.WaterVapour + state.LiquidWater));

    private static double Oxygen(SimulationState state) => (2 * state.Oxygen) + (2 * state.CarbonDioxide) + state.WaterVapour + state.LiquidWater;
}
