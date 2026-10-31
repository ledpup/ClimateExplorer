using System;
using System.Linq;
using ClimateExplorer.CarbonCycle.Calcination;
using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class ValidationTests
{
    [TestMethod]
    public void BurnFuel_NegativeMass_RejectsFuelMass()
    {
        Assert.AreEqual("fuelMass", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.BurnFuel(DefaultFuels.Petrol, -1)));
    }

    [TestMethod]
    public void BurnFuel_NotANumber_RejectsFuelMass()
    {
        Assert.AreEqual("fuelMass", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.BurnFuel(DefaultFuels.Petrol, double.NaN)));
    }

    [TestMethod]
    public void BurnFuel_SplitNotSummingToOne_RejectsCarbonSplit()
    {
        Assert.AreEqual("carbonSplit", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.BurnFuel(DefaultFuels.Petrol, 1, new CarbonSplit(0.9, 0.05, 0, 0))));
    }

    [TestMethod]
    public void BurnFuel_SplitFractionOutsideZeroToOne_RejectsThatFraction()
    {
        var exception = Assert.ThrowsExactly<InputValidationException>(() => Tab1ChemistryEngine.BurnFuel(DefaultFuels.Petrol, 1, new CarbonSplit(1.1, 0, -0.1, 0)));

        CollectionAssert.AreEquivalent(new[] { "carbonSplit.carbonDioxide", "carbonSplit.soot" }, exception.Errors.Select(error => error.Field).ToArray());
    }

    [TestMethod]
    public void WithComponentFraction_OutsideZeroToOne_RejectsThatComponent()
    {
        Assert.AreEqual("fuel.components.sulfur", TestAssert.ThrowsForField(() => DefaultFuels.Petrol.WithComponentFraction("sulfur", 1.5)));
    }

    [TestMethod]
    public void WithComponentFraction_UnknownComponent_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => DefaultFuels.Petrol.WithComponentFraction("lead", 0.1));
    }

    [TestMethod]
    [DataRow(0.0)]
    [DataRow(-1.0)]
    public void WithNetCalorificValue_NotPositive_RejectsNetCalorificValue(double value)
    {
        Assert.AreEqual("fuel.netCalorificValue", TestAssert.ThrowsForField(() => DefaultFuels.Petrol.WithNetCalorificValue(value)));
    }

    [TestMethod]
    public void WithDensity_NotPositive_RejectsDensity()
    {
        Assert.AreEqual("fuel.density", TestAssert.ThrowsForField(() => DefaultFuels.Petrol.WithDensity(0)));
    }

    [TestMethod]
    public void WithNoxFactor_Negative_RejectsNoxFactor()
    {
        Assert.AreEqual("fuel.noxFactor", TestAssert.ThrowsForField(() => DefaultFuels.IdealPetrol.WithNoxFactor(-0.001)));
    }

    [TestMethod]
    public void CreateReal_NoCarbon_RejectsComponents()
    {
        Assert.AreEqual("fuel.components", TestAssert.ThrowsForField(() => Fuel.CreateReal(
            "test",
            "test",
            [FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, 1)],
            9.3e6,
            null,
            0)));
    }

    [TestMethod]
    public void CreateReal_NonFuelElement_RejectsThatComponent()
    {
        Assert.AreEqual("fuel.components.iron", TestAssert.ThrowsForField(() => Fuel.CreateReal(
            "test",
            "test",
            [FuelComponent.FromElement("carbon", "Carbon", Elements.C, 0.9), FuelComponent.FromElement("iron", "Iron", Elements.Fe, 0.1)],
            30e6,
            null,
            0)));
    }

    [TestMethod]
    public void SupplyHeat_Negative_RejectsHeat()
    {
        Assert.AreEqual("heat", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.SupplyHeat(DefaultFuels.Petrol, -1)));
    }

    [TestMethod]
    public void SupplyHeat_AllFuelUnburnt_RejectsCarbonSplit()
    {
        Assert.AreEqual("carbonSplit", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.SupplyHeat(DefaultFuels.Petrol, 1e6, new CarbonSplit(0, 0, 0, 1))));
    }

    [TestMethod]
    public void CalcineRock_NegativeMass_RejectsRockMass()
    {
        Assert.AreEqual("rockMass", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.CalcineRock(RockMaterial.Clinker, -1, DefaultFuels.IdealCoal)));
    }

    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.2)]
    public void CalcineRock_ClinkerCalciumOxideOutOfRange_RejectsIt(double fraction)
    {
        var assumptions = CalcinationAssumptions.Default with { Clinker = CalcinationProduct.Clinker with { CaOMassFraction = fraction } };

        Assert.AreEqual("product.clinker.caOMassFraction", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.CalcineRock(RockMaterial.Clinker, 1, DefaultFuels.IdealCoal, assumptions)));
    }

    [TestMethod]
    public void CalcineRock_ClinkerRatioZero_RejectsClinkerRatio()
    {
        var assumptions = CalcinationAssumptions.Default with { CementClinkerRatio = 0 };

        Assert.AreEqual("cementClinkerRatio", TestAssert.ThrowsForField(() => Tab1ChemistryEngine.CalcineRock(RockMaterial.Cement, 1, DefaultFuels.IdealCoal, assumptions)));
    }
}
