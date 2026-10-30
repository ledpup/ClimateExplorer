using ClimateExplorer.CarbonCycle.Calcination;
using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Fuels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class CalcinationTests
{
    [TestMethod]
    public void Calcine_OneKgLimestone_Gives044KgCarbonDioxide()
    {
        var stream = CalcinationCalculator.Calcine(RockMaterial.Limestone, 1, CalcinationAssumptions.Default);

        Assert.AreEqual(0.44, stream.ProductMass(Material.CarbonDioxide), 0.001);
        Assert.AreEqual(1, stream.ReactantMass(Material.CalciumCarbonate), 1e-12);
    }

    [TestMethod]
    public void Calcine_OneTonneClinkerAt65PercentCalciumOxide_GivesAbout051TonnesCarbonDioxide()
    {
        var stream = CalcinationCalculator.Calcine(RockMaterial.Clinker, 1000, CalcinationAssumptions.Default);

        Assert.AreEqual(0.65, CalcinationAssumptions.Default.Clinker.CaOMassFraction);
        Assert.AreEqual(510, stream.ProductMass(Material.CarbonDioxide), 2);
        Assert.AreEqual(650, stream.ProductMass(Material.CalciumOxide), 1e-9);
    }

    [TestMethod]
    public void Calcine_OneTonnePureCalciumOxideLime_Gives0785TonnesCarbonDioxide()
    {
        var pureLime = CalcinationAssumptions.Default with { Lime = new CalcinationProduct("product.lime.pure", "Pure lime", 1) };

        var stream = CalcinationCalculator.Calcine(RockMaterial.Lime, 1000, pureLime);

        Assert.AreEqual(785, stream.ProductMass(Material.CarbonDioxide), 1);
    }

    [TestMethod]
    public void Calcine_Cement_ScalesClinkerByClinkerRatio()
    {
        var assumptions = CalcinationAssumptions.Default with { CementClinkerRatio = 0.65 };

        var clinker = CalcinationCalculator.Calcine(RockMaterial.Clinker, 1000, assumptions);
        var cement = CalcinationCalculator.Calcine(RockMaterial.Cement, 1000, assumptions);

        Assert.AreEqual(0.65 * clinker.ProductMass(Material.CarbonDioxide), cement.ProductMass(Material.CarbonDioxide), 1e-9);
    }

    [TestMethod]
    public void Calcine_AnyRock_AbsorbsAbout4MegajoulesPerKgRockCarbonDioxide()
    {
        var stream = CalcinationCalculator.Calcine(RockMaterial.Limestone, 1, CalcinationAssumptions.Default);

        var megajoulesPerKg = (stream.EnthalpyChange / 1e6) / stream.ProductMass(Material.CarbonDioxide);

        Assert.AreEqual(4.0, megajoulesPerKg, 0.1);
        Assert.AreEqual(stream.ProductMass(Material.CarbonDioxide) * (12.011 / 44.009), stream.CarbonByFate.CarbonDioxide.FossilKg, 1e-12);
    }

    [TestMethod]
    public void CalcineRock_HeatedWithIdealCoal_AddsAbout045KgFuelCarbonDioxidePerKgRockCarbonDioxide()
    {
        var result = Tab1ChemistryEngine.CalcineRock(RockMaterial.Limestone, 1, DefaultFuels.IdealCoal);

        var rock = result.Stream(Tab1StreamKind.Calcination)!;
        var combustion = result.Stream(Tab1StreamKind.Combustion)!;
        var ratio = combustion.ProductMass(Material.CarbonDioxide) / rock.ProductMass(Material.CarbonDioxide);

        Assert.AreEqual(0.45, ratio, 0.01);
    }

    [TestMethod]
    public void CalcineRock_AnyFuel_SuppliesExactlyTheReactionsHeat()
    {
        var result = Tab1ChemistryEngine.CalcineRock(RockMaterial.Cement, 1000, DefaultFuels.BituminousCoal, split: new CarbonSplit(0.95, 0.03, 0.02, 0));

        var rock = result.Stream(Tab1StreamKind.Calcination)!;
        var combustion = result.Stream(Tab1StreamKind.Combustion)!;

        Assert.HasCount(2, result.Streams);
        Assert.AreSame(combustion, result.Streams[0]);
        Assert.IsGreaterThan(0, rock.EnthalpyChange);
        Assert.AreEqual(-rock.EnthalpyChange, combustion.EnthalpyChange, 1e-6);
        Assert.AreEqual(0, result.Totals.EnthalpyChange, 1e-6);
    }

    [TestMethod]
    public void CalcineRock_Totals_SumCarbonDioxideFromBothStreams()
    {
        var result = Tab1ChemistryEngine.CalcineRock(RockMaterial.Clinker, 1000, DefaultFuels.IdealCoal);

        var expected = result.Streams[0].ProductMass(Material.CarbonDioxide) + result.Streams[1].ProductMass(Material.CarbonDioxide);

        Assert.AreEqual(expected, result.Totals.ProductMass(Material.CarbonDioxide), 1e-9);
        Assert.AreEqual(result.Streams[0].CarbonByFate.Total.TotalKg + result.Streams[1].CarbonByFate.Total.TotalKg, result.Totals.CarbonByFate.Total.TotalKg, 1e-9);
    }
}
