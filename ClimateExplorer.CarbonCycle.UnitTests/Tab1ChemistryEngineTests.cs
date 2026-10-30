using System;
using System.Linq;
using ClimateExplorer.CarbonCycle.Calcination;
using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class Tab1ChemistryEngineTests
{
    [TestMethod]
    [DataRow("fuel.petrol", 0.0)]
    [DataRow("fuel.bituminousCoal", 0.05)]
    [DataRow("fuel.naturalGas.ideal", 0.1)]
    public void SupplyHeat_FuelMassBurnedInFuelMode_GivesBackTheHeat(string fuelId, double soot)
    {
        var fuel = DefaultFuels.All.Single(f => f.Id == fuelId);
        var split = new CarbonSplit(1 - soot, 0, soot, 0);
        const double Heat = 1e9;

        var energyMode = Tab1ChemistryEngine.SupplyHeat(fuel, Heat, split);
        var fuelMass = energyMode.Totals.ReactantMass(Material.Fuel);
        var fuelMode = Tab1ChemistryEngine.BurnFuel(fuel, fuelMass, split);

        TestAssert.AreWithin(-Heat, energyMode.Totals.EnthalpyChange, 1e-12);
        TestAssert.AreWithin(-Heat, fuelMode.Totals.EnthalpyChange, 1e-12);
    }

    [TestMethod]
    public void SupplyHeat_SmokyFlame_NeedsMoreFuel()
    {
        var clean = Tab1ChemistryEngine.SupplyHeat(DefaultFuels.Diesel, 1e9);
        var smoky = Tab1ChemistryEngine.SupplyHeat(DefaultFuels.Diesel, 1e9, new CarbonSplit(0.9, 0.05, 0.05, 0));

        Assert.IsGreaterThan(clean.Totals.ReactantMass(Material.Fuel), smoky.Totals.ReactantMass(Material.Fuel));
    }

    [TestMethod]
    public void HeatReleasedPerKg_CompleteCombustion_IsNetCalorificValue()
    {
        Assert.AreEqual(DefaultFuels.Petrol.NetCalorificValue, Tab1ChemistryEngine.HeatReleasedPerKg(DefaultFuels.Petrol), 1e-6);
    }

    [TestMethod]
    public void BurnFuel_IdealAndRealSideBySide_RealCoalGivesLessCarbonDioxideAndHeat()
    {
        // Tab 1 plan §2.3: pure carbon 3.66 kg CO₂ and 32.8 MJ; real bituminous coal ~2.4–2.7 kg CO₂.
        var ideal = Tab1ChemistryEngine.BurnFuel(DefaultFuels.IdealCoal, 1).Totals;
        var real = Tab1ChemistryEngine.BurnFuel(DefaultFuels.BituminousCoal, 1).Totals;

        Assert.AreEqual(3.66, ideal.ProductMass(Material.CarbonDioxide), 0.01);
        Assert.IsTrue(real.ProductMass(Material.CarbonDioxide) is >= 2.4 and <= 2.7, real.ProductMass(Material.CarbonDioxide).ToString());
        Assert.IsGreaterThan(ideal.EnthalpyChange, real.EnthalpyChange);
    }

    [TestMethod]
    public void BurnFuel_NoSplit_IsCompleteCombustion()
    {
        var result = Tab1ChemistryEngine.BurnFuel(DefaultFuels.IdealPetrol, 1);

        Assert.HasCount(1, result.Streams);
        Assert.AreEqual(Tab1StreamKind.Total, result.Totals.Kind);
        Assert.AreEqual(0, result.Totals.ProductMass(Material.CarbonMonoxide));
        Assert.AreEqual("2 C₈H₁₈ + 25 O₂ → 16 CO₂ + 18 H₂O", result.Totals.Reactions[0].ToEquationString());
        Assert.IsTrue(result.Totals.Reactions[0].HasSameNetStoichiometry(ReactionCatalog.OctaneCombustion));
    }

    [TestMethod]
    public void CalcineRock_NoAssumptions_UsesDefaults()
    {
        var withDefaults = Tab1ChemistryEngine.CalcineRock(RockMaterial.Cement, 1000, DefaultFuels.IdealCoal);
        var explicitDefaults = Tab1ChemistryEngine.CalcineRock(RockMaterial.Cement, 1000, DefaultFuels.IdealCoal, CalcinationAssumptions.Default, CarbonSplit.Complete);

        Assert.AreEqual(explicitDefaults.Totals.ProductMass(Material.CarbonDioxide), withDefaults.Totals.ProductMass(Material.CarbonDioxide));
    }
}
