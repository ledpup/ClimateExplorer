using System;
using ClimateExplorer.CarbonCycle.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class PlanetEnergyBalanceTests
{
    // J/(m²·K): Earth's air alone, as the dead planet's
    private const double HeatCapacity = 1.01e7;

    private const double Year = 365.25 * 24 * 3600;

    private static readonly PlanetEnergyBalance DryPlanet = new(PlanetEnergySettings.DryPlanet);

    [TestMethod]
    public void StartTemperature_DryPlanet_MatchesClosedFormAndIsInBalance()
    {
        var absorbed = 1361.0 / 4 * (1 - 0.30);
        var expected = Math.Pow(absorbed / (0.9 * PlanetEnergyBalance.StefanBoltzmann), 0.25);

        Assert.AreEqual(238.175, DryPlanet.AbsorbedSunlight, 1e-9);
        Assert.AreEqual(expected, DryPlanet.StartTemperature, 1e-9);
        Assert.AreEqual(261.4, DryPlanet.StartTemperature, 0.1);
        Assert.AreEqual(0, DryPlanet.Budget(DryPlanet.StartTemperature, NoForcing()).Imbalance, 1e-9);
    }

    [TestMethod]
    public void StartTemperature_NoGreenhouseEffect_Earths255Kelvin()
    {
        var bare = new PlanetEnergyBalance(PlanetEnergySettings.DryPlanet with { GreenhouseEffect = 0 });

        Assert.AreEqual(254.6, bare.StartTemperature, 0.1);
    }

    [TestMethod]
    public void StartTemperature_MoreSunlightLessAlbedoOrMoreGreenhouseEffect_Warmer()
    {
        var start = DryPlanet.StartTemperature;
        var settings = PlanetEnergySettings.DryPlanet;

        Assert.IsGreaterThan(start, new PlanetEnergyBalance(settings with { SolarConstant = 1400 }).StartTemperature);
        Assert.IsGreaterThan(start, new PlanetEnergyBalance(settings with { Albedo = 0.25 }).StartTemperature);
        Assert.IsGreaterThan(start, new PlanetEnergyBalance(settings with { GreenhouseEffect = 0.2 }).StartTemperature);
    }

    [TestMethod]
    public void Feedback_NoWaterVapour_PlanckResponse()
    {
        var t = DryPlanet.StartTemperature;

        Assert.AreEqual(4 * DryPlanet.HeatToSpace(t, 0) / t, DryPlanet.Feedback(t), 1e-9);
        Assert.AreEqual(3.64, DryPlanet.Feedback(t), 0.01);
        Assert.AreEqual(1.08, DryPlanet.WarmingPerDoubling, 0.01);
    }

    [TestMethod]
    public void WarmingPerDoubling_WithWaterVapourFeedback_Larger()
    {
        var wet = new PlanetEnergyBalance(PlanetEnergySettings.DryPlanet with { WaterVapourFeedback = 0.003 });

        Assert.IsGreaterThan(DryPlanet.WarmingPerDoubling, wet.WarmingPerDoubling);
    }

    [TestMethod]
    public void Temperature_ConstantForcing_SettlesWhereInBalance()
    {
        var forcing = CarbonDioxide(3.93);

        var t = DryPlanet.StartTemperature;
        for (var i = 0; i < 100; i++)
        {
            t = DryPlanet.Temperature(t, forcing, HeatCapacity, 0.1 * Year);
        }

        // (1 − g₀) σT⁴ = ASR + F
        var expected = Math.Pow((DryPlanet.AbsorbedSunlight + 3.93) / (0.9 * PlanetEnergyBalance.StefanBoltzmann), 0.25);
        Assert.AreEqual(expected, t, 1e-6);
        Assert.AreEqual(0, DryPlanet.Budget(t, forcing).Imbalance, 1e-6);
        Assert.AreEqual(DryPlanet.WarmingPerDoubling, t - DryPlanet.StartTemperature, 0.02);
    }

    [TestMethod]
    public void Temperature_NonRadiativeHeat_WarmsButIsntInTheImbalance()
    {
        var heat = new ForcingBreakdown([], 1);

        var t = DryPlanet.Temperature(DryPlanet.StartTemperature, heat, HeatCapacity, 10 * Year);

        Assert.IsGreaterThan(DryPlanet.StartTemperature, t);
        Assert.AreEqual(-1, DryPlanet.Budget(t, heat).Imbalance, 0.01);
    }

    [TestMethod]
    public void Temperature_OneLargeStepOrManySmallOnes_Agree()
    {
        // Half a kelvin from balance, as far as DeadPlanet lets a substep go
        var forcing = CarbonDioxide(1.8);

        var large = DryPlanet.Temperature(DryPlanet.StartTemperature, forcing, HeatCapacity, Year);

        var small = DryPlanet.StartTemperature;
        for (var i = 0; i < 1000; i++)
        {
            small = DryPlanet.Temperature(small, forcing, HeatCapacity, Year / 1000);
        }

        Assert.AreEqual(small, large, 0.01);
    }

    [TestMethod]
    public void Temperature_SteadilyRisingForcing_LagsTheBalanceByTheResponseTime()
    {
        // Rising 0.04 W/m² a year, about the dead planet's after a century
        var rate = 0.04 / Year;
        var t = DryPlanet.StartTemperature;

        for (var i = 0; i < 100; i++)
        {
            t = DryPlanet.Temperature(t, CarbonDioxide(rate * i * 0.1 * Year), CarbonDioxide(rate * (i + 1) * 0.1 * Year), HeatCapacity, 0.1 * Year);
        }

        // N = rate × C/λ, the forcing added over one response time
        var lambda = DryPlanet.Feedback(t);
        Assert.AreEqual(rate * HeatCapacity / lambda, DryPlanet.Budget(t, CarbonDioxide(rate * 10 * Year)).Imbalance, 1e-4);
    }

    [TestMethod]
    public void Temperature_RisingForcingInOneLongStep_SameLagAsManyShortOnes()
    {
        var start = CarbonDioxide(0);
        var end = CarbonDioxide(0.4);

        var large = DryPlanet.Temperature(DryPlanet.StartTemperature, start, end, HeatCapacity, 10 * Year);

        var small = DryPlanet.StartTemperature;
        for (var i = 0; i < 1000; i++)
        {
            small = DryPlanet.Temperature(small, CarbonDioxide(0.4 * i / 1000), CarbonDioxide(0.4 * (i + 1) / 1000), HeatCapacity, Year / 100);
        }

        Assert.AreEqual(small, large, 1e-4);
    }

    [TestMethod]
    public void Budget_DryPlanet_ReflectsAlbedosShare()
    {
        var budget = DryPlanet.Budget(DryPlanet.StartTemperature, NoForcing());

        Assert.AreEqual(340.25, budget.IncomingSunlight, 1e-9);
        Assert.AreEqual(0.30 * 340.25, budget.Reflected, 1e-9);
        Assert.AreEqual(budget.IncomingSunlight, budget.Reflected + budget.Absorbed, 1e-9);
    }

    private static ForcingBreakdown NoForcing() => new([], 0);

    private static ForcingBreakdown CarbonDioxide(double value) => new([new(ForcingAgents.CarbonDioxide, value)], 0);
}
