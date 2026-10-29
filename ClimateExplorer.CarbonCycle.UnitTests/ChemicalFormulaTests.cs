using System;
using System.Linq;
using ClimateExplorer.CarbonCycle.Chemistry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class ChemicalFormulaTests
{
    [TestMethod]
    [DataRow("CO2", 44.01)]
    [DataRow("H2O", 18.02)]
    [DataRow("CH4", 16.04)]
    [DataRow("C8H18", 114.23)]
    [DataRow("CaCO3", 100.09)]
    [DataRow("SO2", 64.06)]
    public void MolarMass_KnownFormula_MatchesStandardValue(string formula, double expected)
    {
        Assert.AreEqual(expected, ChemicalFormula.Parse(formula).MolarMass, 0.01);
    }

    [TestMethod]
    public void MolarMass_CarbonDioxideOverCarbon_Is3664()
    {
        Assert.AreEqual(3.664, SpeciesCatalog.CarbonDioxide.MolarMass / SpeciesCatalog.Carbon.MolarMass, 0.001);
    }

    [TestMethod]
    public void MolarMass_SulfurDioxideOverSulfur_IsAboutTwo()
    {
        Assert.AreEqual(2.0, SpeciesCatalog.SulfurDioxide.MolarMass / SpeciesCatalog.Sulfur.MolarMass, 0.01);
    }

    [TestMethod]
    public void MolarMass_CarbonMonoxideOverCarbon_IsAbout28Over12()
    {
        Assert.AreEqual(28.0 / 12.0, SpeciesCatalog.CarbonMonoxide.MolarMass / SpeciesCatalog.Carbon.MolarMass, 0.002);
    }

    [TestMethod]
    public void Parse_IronOxide_GivesElementCounts()
    {
        var formula = ChemicalFormula.Parse("Fe2O3");

        Assert.AreEqual(2, formula.CountOf(Elements.Fe));
        Assert.AreEqual(3, formula.CountOf(Elements.O));
        Assert.HasCount(2, formula.Atoms);
    }

    [TestMethod]
    public void Parse_RepeatedElement_SumsCountsAndKeepsAuthoredText()
    {
        var formula = ChemicalFormula.Parse("C2H5OH");

        Assert.AreEqual(6, formula.CountOf(Elements.H));
        Assert.AreEqual(1, formula.CountOf(Elements.O));
        Assert.AreEqual("C₂H₅OH", formula.ToDisplayString());
    }

    [TestMethod]
    public void Parse_CalciumCarbonate_KeepsChemistsOrder()
    {
        var formula = ChemicalFormula.Parse("CaCO3");

        CollectionAssert.AreEqual(new[] { "Ca", "C", "O" }, formula.Atoms.Select(atom => atom.Element.Symbol).ToArray());
        Assert.AreEqual("CaCO₃", formula.ToDisplayString());
    }

    [TestMethod]
    [DataRow("Xx2")]
    [DataRow("Mg")]
    [DataRow("co2")]
    [DataRow("C0")]
    [DataRow("")]
    public void Parse_InvalidFormula_Throws(string text)
    {
        Assert.ThrowsExactly<ArgumentException>(() => ChemicalFormula.Parse(text));
    }

    [TestMethod]
    public void FromAtoms_FractionalCounts_FormatsThreeSignificantFigures()
    {
        var formula = ChemicalFormula.FromAtoms([new(Elements.C, 1), new(Elements.H, 1.9561), new(Elements.S, 0.0000043612)]);

        Assert.AreEqual("CH1.96S0.00000436", formula.Text);
        Assert.AreEqual("CH₁.₉₆S₀.₀₀₀₀₀₄₃₆", formula.ToDisplayString());
    }

    [TestMethod]
    public void ToDisplayString_WithPhase_AppendsPhase()
    {
        Assert.AreEqual("H₂O(g)", SpeciesCatalog.WaterVapour.ToDisplayString(includePhase: true));
        Assert.AreEqual("H₂SO₄(aq)", SpeciesCatalog.SulfuricAcid.ToDisplayString(includePhase: true));
    }

    [TestMethod]
    public void SpeciesCatalog_Ids_AreUnique()
    {
        Assert.AreEqual(SpeciesCatalog.All.Count, SpeciesCatalog.All.Select(species => species.Id).Distinct().Count());
    }
}
