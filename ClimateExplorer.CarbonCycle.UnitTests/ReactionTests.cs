using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.CarbonCycle.Chemistry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class ReactionTests
{
    public static IEnumerable<object[]> CatalogReactions => ReactionCatalog.All.Select(reaction => new object[] { reaction.Id });

    [TestMethod]
    [DynamicData(nameof(CatalogReactions))]
    public void IsBalanced_CatalogReaction_IsBalancedInAtomsAndMass(string id)
    {
        var reaction = ReactionCatalog.All.Single(r => r.Id == id);

        Assert.IsTrue(reaction.IsBalanced(), reaction.ToEquationString());
        TestAssert.AreWithin(reaction.ReactantMass, reaction.ProductMass, 1e-9, reaction.ToEquationString());
    }

    [TestMethod]
    public void CatalogReactions_Ids_AreUnique()
    {
        Assert.AreEqual(ReactionCatalog.All.Count, ReactionCatalog.All.Select(reaction => reaction.Id).Distinct().Count());
    }

    // Tab 1 plan §2.2, §3 and §4
    [TestMethod]
    [DataRow("combustion.carbon.complete", -393.5)]
    [DataRow("combustion.methane.complete", -802)]
    [DataRow("combustion.methane.complete.gross", -890)]
    [DataRow("combustion.octane.complete", -5074)]
    [DataRow("calcination.calciumCarbonate", 178)]
    [DataRow("nox.thermal", 180.6)]
    [DataRow("combustion.sulfur", -296.8)]
    [DataRow("combustion.methane.carbonMonoxide", -519)]
    [DataRow("combustion.methane.soot", -408)]
    [DataRow("combustion.carbon.carbonMonoxide", -110.5)]
    [DataRow("oxidation.carbonMonoxide", -283.0)]
    [DataRow("nox.oxidation", -114)]
    [DataRow("reforming.steamMethane", 165)]
    public void EnthalpyChange_CatalogReaction_MatchesTab1PlanWithinOnePercent(string id, double expected)
    {
        var reaction = ReactionCatalog.All.Single(r => r.Id == id);

        TestAssert.AreWithin(expected, reaction.EnthalpyChange!.Value, 0.01);
    }

    [TestMethod]
    public void EnthalpyChange_SpeciesWithoutEnthalpy_IsAbsent()
    {
        Assert.IsNull(ReactionCatalog.CarbonMonoxideHydroxyl.EnthalpyChange);
        Assert.IsNull(ReactionCatalog.SulfateAqueous.EnthalpyChange);
    }

    [TestMethod]
    public void Add_CarbonToCarbonMonoxideThenToCarbonDioxide_EqualsCarbonCombustion()
    {
        var sum = ReactionCatalog.CarbonToCarbonMonoxide.Add(ReactionCatalog.CarbonMonoxideOxidation);

        Assert.IsTrue(sum.HasSameNetStoichiometry(ReactionCatalog.CarbonCombustion), sum.ToEquationString());
        Assert.IsFalse(sum.Involves(SpeciesCatalog.CarbonMonoxide));
        Assert.AreEqual(ReactionCatalog.CarbonCombustion.EnthalpyChange!.Value, sum.EnthalpyChange!.Value, 1e-9);
    }

    [TestMethod]
    public void Add_IntermediateOnBothSides_CancelsIt()
    {
        // N₂ + O₂ → 2 NO, then 2 NO + O₂ → 2 NO₂: the NO cancels.
        var sum = ReactionCatalog.ThermalNitricOxide.Add(ReactionCatalog.NitricOxideOxidation);

        Assert.AreEqual("N₂ + 2 O₂ → 2 NO₂", sum.ToEquationString());
        Assert.IsTrue(sum.IsBalanced());
    }

    [TestMethod]
    public void Add_WithMultiple_ScalesTheOtherReaction()
    {
        // C + ½ O₂ → CO, plus twice CO + ½ O₂ → CO₂: one CO more is used than is made.
        var sum = ReactionCatalog.CarbonToCarbonMonoxide.Add(ReactionCatalog.CarbonMonoxideOxidation, 2);

        Assert.AreEqual("2 C + 3 O₂ + 2 CO → 4 CO₂", sum.ToEquationString());
        Assert.IsTrue(sum.IsBalanced());
    }

    [TestMethod]
    public void ToEquationString_Octane_ShowsSmallestWholeNumbers()
    {
        Assert.AreEqual("2 C₈H₁₈ + 25 O₂ → 16 CO₂ + 18 H₂O", ReactionCatalog.OctaneCombustion.ToEquationString());
    }

    [TestMethod]
    public void ToEquationString_HalfCoefficient_DoublesToWholeNumbers()
    {
        Assert.AreEqual("2 C + O₂ → 2 CO", ReactionCatalog.CarbonToCarbonMonoxide.ToEquationString());
    }

    [TestMethod]
    public void ToEquationString_WithPhases_ShowsPhases()
    {
        Assert.AreEqual("CH₄(g) + 2 O₂(g) → CO₂(g) + 2 H₂O(l)", ReactionCatalog.MethaneCombustionGross.ToEquationString(includePhases: true));
    }

    [TestMethod]
    public void ToEquationString_NoWholeNumberForm_ShowsRoundedDecimals()
    {
        var reaction = new Reaction(
            "test",
            "test",
            [new(1, SpeciesCatalog.Methane), new(1.9876543, SpeciesCatalog.Oxygen)],
            [new(0.9876543, SpeciesCatalog.CarbonDioxide), new(0.0123457, SpeciesCatalog.CarbonMonoxide), new(2, SpeciesCatalog.WaterVapour)]);

        Assert.AreEqual("CH₄ + 1.99 O₂ → 0.988 CO₂ + 0.0123 CO + 2 H₂O", reaction.ToEquationString());
    }

    [TestMethod]
    public void ScaleByMass_OneKgLimestone_GivesCarbonDioxideAndHeat()
    {
        var scaled = ReactionCatalog.Calcination.ScaleByMass(SpeciesCatalog.CalciumCarbonate, 1);

        Assert.AreEqual(0.44, scaled.Of(SpeciesCatalog.CarbonDioxide).MassKg, 0.001);
        Assert.AreEqual(1.0, scaled.Reactants.Sum(term => term.MassKg), 1e-12);
        Assert.AreEqual(1.0, scaled.Products.Sum(term => term.MassKg), 1e-12);

        // 1.78 MJ per kg of CaCO₃ taken in (Tab 1 §2.2)
        TestAssert.AreWithin(1.78e6, scaled.EnthalpyChange!.Value, 0.01);
    }

    [TestMethod]
    public void ScaleByMoles_Product_ScalesEveryParticipant()
    {
        var scaled = ReactionCatalog.OctaneCombustion.ScaleByMoles(SpeciesCatalog.CarbonDioxide, 16);

        Assert.AreEqual(2, scaled.Extent, 1e-12);
        Assert.AreEqual(2, scaled.Of(SpeciesCatalog.Octane).Moles, 1e-12);
        Assert.AreEqual(25, scaled.Of(SpeciesCatalog.Oxygen).Moles, 1e-12);
        Assert.AreEqual(18, scaled.Of(SpeciesCatalog.WaterVapour).Moles, 1e-12);
    }

    [TestMethod]
    public void ScaleByHeat_KnownHeat_GivesExtent()
    {
        var perMoleJoules = ReactionCatalog.MethaneCombustion.EnthalpyChange!.Value * 1000;

        var scaled = ReactionCatalog.MethaneCombustion.ScaleByHeat(3 * perMoleJoules);

        Assert.AreEqual(3, scaled.Of(SpeciesCatalog.Methane).Moles, 1e-12);
    }

    [TestMethod]
    public void ScaleByHeat_OppositeSign_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ReactionCatalog.MethaneCombustion.ScaleByHeat(1e6));
    }

    [TestMethod]
    public void ScaleByHeat_NoEnthalpy_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => ReactionCatalog.SulfateAqueous.ScaleByHeat(-1));
    }

    [TestMethod]
    public void ScaleByMass_SpeciesNotInReaction_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ReactionCatalog.Calcination.ScaleByMass(SpeciesCatalog.Methane, 1));
    }

    [TestMethod]
    public void Normalise_ToOxygen_GivesOxygenCoefficientOne()
    {
        var reaction = ReactionCatalog.OctaneCombustion.Normalise(SpeciesCatalog.Oxygen);

        Assert.AreEqual(1, reaction.CoefficientOf(SpeciesCatalog.Oxygen), 1e-12);
        Assert.AreEqual(0.08, reaction.CoefficientOf(SpeciesCatalog.Octane), 1e-12);
        Assert.AreEqual(ReactionCatalog.OctaneCombustion.EnthalpyChange!.Value / 12.5, reaction.EnthalpyChange!.Value, 1e-9);
    }

    [TestMethod]
    public void Constructor_NonPositiveCoefficient_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Reaction("bad", "bad", [new(0, SpeciesCatalog.Carbon)], [new(1, SpeciesCatalog.Carbon)]));
    }
}
