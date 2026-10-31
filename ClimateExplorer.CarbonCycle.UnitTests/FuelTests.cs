using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class FuelTests
{
    public static IEnumerable<object[]> AllFuels => DefaultFuels.All.Select(fuel => new object[] { fuel.Id });

    // Tab 1 plan §2.2
    [TestMethod]
    [DataRow("fuel.coal.ideal", 32.8)]
    [DataRow("fuel.naturalGas.ideal", 50.0)]
    [DataRow("fuel.petrol.ideal", 44.4)]
    public void NetCalorificValue_IdealFuel_IsDerivedFromChemistry(string id, double expectedMegajoulesPerKg)
    {
        var fuel = DefaultFuels.All.Single(f => f.Id == id);

        Assert.AreEqual(expectedMegajoulesPerKg, fuel.NetCalorificValue / 1e6, 0.05);
        Assert.AreEqual(fuel.NetCalorificValue, fuel.DerivedNetCalorificValue!.Value);
    }

    [TestMethod]
    [DataRow("fuel.naturalGas.ideal", 55)]
    [DataRow("fuel.petrol.ideal", 69)]
    [DataRow("fuel.coal.ideal", 112)]
    public void CarbonDioxidePerMegajoule_IdealFuel_MatchesTab1Plan(string id, double expectedGrams)
    {
        var fuel = DefaultFuels.All.Single(f => f.Id == id);
        var result = Tab1ChemistryEngine.BurnFuel(fuel, 1);

        var gramsPerMegajoule = (result.Totals.ProductMass(Material.CarbonDioxide) * 1000) / (fuel.NetCalorificValue / 1e6);

        TestAssert.AreWithin(expectedGrams, gramsPerMegajoule, 0.02);
    }

    [TestMethod]
    public void HydrogenToCarbonRatio_RealPetrol_IsBelowOctaneInRealBlendRange()
    {
        var ratio = DefaultFuels.Petrol.HydrogenToCarbonRatio;

        Assert.AreEqual(2.25, DefaultFuels.IdealPetrol.HydrogenToCarbonRatio, 1e-12);
        Assert.IsLessThan(2.25, ratio);
        Assert.IsTrue(ratio is >= 1.8 and <= 2.0, $"H:C {ratio}");
    }

    [TestMethod]
    public void PerCarbonFormula_RealPetrol_ShowsHydrogenPerCarbon()
    {
        var formula = DefaultFuels.Petrol.PerCarbonFormula;

        Assert.AreEqual(1, formula.CountOf(Elements.C));
        Assert.AreEqual(DefaultFuels.Petrol.HydrogenToCarbonRatio, formula.CountOf(Elements.H), 1e-12);
        Assert.StartsWith("CH₁.₉₆", formula.ToDisplayString());
    }

    [TestMethod]
    [DynamicData(nameof(AllFuels))]
    public void Components_DefaultFuel_SumToOneAndContainCarbon(string id)
    {
        var fuel = DefaultFuels.All.Single(f => f.Id == id);

        Assert.AreEqual(1, fuel.Components.Sum(component => component.MassFraction), 1e-9);
        Assert.IsGreaterThan(0, fuel.CarbonMassFraction);
        Assert.IsGreaterThan(0, fuel.NetCalorificValue);
    }

    [TestMethod]
    public void CreateReal_FractionsNotSummingToOne_IsRejected()
    {
        var field = TestAssert.ThrowsForField(() => Fuel.CreateReal(
            "test",
            "test",
            [FuelComponent.Molecule("methane", "Methane", SpeciesCatalog.Methane, 0.5), FuelComponent.Molecule("ethane", "Ethane", SpeciesCatalog.Ethane, 0.4)],
            48e6,
            null,
            0));

        Assert.AreEqual("fuel.components", field);
    }

    [TestMethod]
    public void WithComponentFraction_MoreSulfur_LargestComponentTakesUpTheDifference()
    {
        var edited = DefaultFuels.Petrol.WithComponentFraction("sulfur", 0.00005);

        Assert.AreEqual(0.00005, Fraction(edited, "sulfur"), 1e-15);
        Assert.AreEqual(0.25, Fraction(edited, "aromatics"), 1e-15);
        Assert.AreEqual(0.74995, Fraction(edited, "alkanes"), 1e-12);
        Assert.AreEqual(1, edited.Components.Sum(component => component.MassFraction), 1e-12);
    }

    [TestMethod]
    public void WithComponentFraction_LargestCantTakeUpTheDifference_RescalesTheOthers()
    {
        var edited = DefaultFuels.BituminousCoal.WithComponentFraction("ash", 0.9);

        // The others were 0.887 in total and are scaled to 0.1; carbon alone (0.666) couldn't give up 0.787.
        Assert.AreEqual(0.9, Fraction(edited, "ash"), 1e-15);
        Assert.AreEqual(0.666 * 0.1 / 0.887, Fraction(edited, "carbon"), 1e-12);
        Assert.AreEqual(0.08 * 0.1 / 0.887, Fraction(edited, "moisture"), 1e-12);
        Assert.AreEqual(1, edited.Components.Sum(component => component.MassFraction), 1e-12);
    }

    [TestMethod]
    public void WithComponentFraction_IdealFuel_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => DefaultFuels.IdealPetrol.WithComponentFraction("C8H18(l)", 0.5));
    }

    [TestMethod]
    public void WithNetCalorificValue_IdealFuel_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => DefaultFuels.IdealPetrol.WithNetCalorificValue(40e6));
    }

    [TestMethod]
    public void WithNoxFactor_RealFuel_KeepsEverythingElse()
    {
        var edited = DefaultFuels.Diesel.WithNoxFactor(0.03);

        Assert.AreEqual(0.03, edited.NoxFactor);
        Assert.AreEqual(DefaultFuels.Diesel.NetCalorificValue, edited.NetCalorificValue);
        Assert.AreEqual(DefaultFuels.Diesel.CarbonMassFraction, edited.CarbonMassFraction);
        Assert.AreSame(DefaultFuels.IdealDiesel, edited.IdealCounterpart);
    }

    [TestMethod]
    [DataRow("fuel.naturalGas")]
    [DataRow("fuel.jetFuel")]
    [DataRow("fuel.lpg")]
    public void Burn_EachComponentSeparately_GivesSameProductsAsElementTotals(string id)
    {
        var fuel = DefaultFuels.RealFuels.Single(f => f.Id == id);
        const double FuelMass = 10;

        var carbonDioxide = 0.0;
        var water = 0.0;
        var nitrogen = 0.0;
        var sulfurDioxide = 0.0;
        var oxygen = 0.0;

        foreach (var component in fuel.Components)
        {
            var mass = component.MassFraction * FuelMass;

            if (component.Kind == FuelComponentKind.Element)
            {
                Assert.AreSame(Elements.S, component.Element);
                var sulfur = ReactionCatalog.SulfurCombustion.ScaleByMass(SpeciesCatalog.Sulfur, mass);
                sulfurDioxide += sulfur.Of(SpeciesCatalog.SulfurDioxide).MassKg;
                oxygen += sulfur.Of(SpeciesCatalog.Oxygen).MassKg;
                continue;
            }

            var reaction = CombustionReactions.Complete(component.Species!);

            if (reaction is null)
            {
                // Nothing to burn: CO₂ and N₂ pass through.
                carbonDioxide += component.Species == SpeciesCatalog.CarbonDioxide ? mass : 0;
                nitrogen += component.Species == SpeciesCatalog.Nitrogen ? mass : 0;
                continue;
            }

            var scaled = reaction.ScaleByMass(component.Species!, mass);
            carbonDioxide += MassOrZero(scaled, SpeciesCatalog.CarbonDioxide);
            water += MassOrZero(scaled, SpeciesCatalog.WaterVapour);
            sulfurDioxide += MassOrZero(scaled, SpeciesCatalog.SulfurDioxide);
            oxygen += MassOrZero(scaled, SpeciesCatalog.Oxygen);
        }

        var result = Tab1ChemistryEngine.BurnFuel(fuel, FuelMass).Totals;

        Assert.AreEqual(carbonDioxide, result.ProductMass(Material.CarbonDioxide), 1e-9);
        Assert.AreEqual(water, result.ProductMass(Material.Water), 1e-9);
        Assert.AreEqual(nitrogen, result.ProductMass(Material.Nitrogen), 1e-9);
        Assert.AreEqual(sulfurDioxide, result.ProductMass(Material.SulfurDioxide), 1e-9);
        Assert.AreEqual(oxygen, result.ReactantMass(Material.Oxygen), 1e-9);
    }

    [TestMethod]
    public void BurnFuel_E10_ReportsEthanolCarbonAsFastCycle()
    {
        var e10 = Fuel.CreateReal(
            "fuel.petrol.e10",
            "Petrol (E10)",
            [
                FuelComponent.Molecule("alkanes", "Alkanes (as octane)", SpeciesCatalog.Octane, 0.67509),
                FuelComponent.Molecule("aromatics", "Aromatics (as toluene)", SpeciesCatalog.Toluene, 0.225),
                FuelComponent.Molecule("ethanol", "Ethanol", SpeciesCatalog.Ethanol, 0.0999, CarbonOrigin.FastCycle),
                FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, 0.00001),
            ],
            42.6e6,
            745,
            0.008);
        var ethanolCarbonFraction = (2 * Elements.C.StandardAtomicWeight) / SpeciesCatalog.Ethanol.MolarMass;

        var carbon = Tab1ChemistryEngine.BurnFuel(e10, 1).Totals.CarbonByFate.CarbonDioxide;

        Assert.AreEqual(0.0999 * ethanolCarbonFraction, carbon.FastCycleKg, 1e-12);
        Assert.AreEqual(e10.CarbonMassFraction - (0.0999 * ethanolCarbonFraction), carbon.FossilKg, 1e-12);
    }

    [TestMethod]
    public void DerivedNetCalorificValue_AllMoleculeFuel_IsPresentAndCloseToSourcedValue()
    {
        var fuel = DefaultFuels.NaturalGas;

        TestAssert.AreWithin(fuel.NetCalorificValue, fuel.DerivedNetCalorificValue!.Value, 0.03);
    }

    [TestMethod]
    public void DerivedNetCalorificValue_MoleculesPlusTraceSulfur_IsPresent()
    {
        Assert.IsNotNull(DefaultFuels.Petrol.DerivedNetCalorificValue);
    }

    [TestMethod]
    public void DerivedNetCalorificValue_ElementWrittenFuel_IsAbsent()
    {
        Assert.IsNull(DefaultFuels.BituminousCoal.DerivedNetCalorificValue);
        Assert.IsNull(DefaultFuels.HeavyFuelOil.DerivedNetCalorificValue);
    }

    [TestMethod]
    public void DerivedNetCalorificValue_ComponentWithoutEnthalpy_IsAbsent()
    {
        // Diesel's methylnaphthalene has no sourced enthalpy yet; that disables the check, not the fuel.
        Assert.IsNull(DefaultFuels.Diesel.DerivedNetCalorificValue);
        Assert.AreEqual(43.0e6, DefaultFuels.Diesel.NetCalorificValue);
    }

    [TestMethod]
    public void CreateIdeal_MoleculeWithoutEnthalpy_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Fuel.CreateIdeal("test", "test", SpeciesCatalog.Methylnaphthalene, 1000));
    }

    private static double Fraction(Fuel fuel, string componentId) => fuel.Components.Single(component => component.Id == componentId).MassFraction;

    private static double MassOrZero(ScaledReaction scaled, Species species) =>
        scaled.Reaction.Involves(species) ? scaled.Of(species).MassKg : 0;
}
