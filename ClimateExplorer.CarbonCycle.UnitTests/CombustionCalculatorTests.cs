using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class CombustionCalculatorTests
{
    private static readonly CarbonSplit[] Splits =
    [
        CarbonSplit.Complete,
        new(0.97, 0.02, 0.01, 0),
        new(0.5, 0.3, 0.1, 0.1),
        new(0, 1, 0, 0),
        new(0, 0, 1, 0),
        new(0, 0, 0, 1),
        new(0.25, 0.25, 0.25, 0.25),
    ];

    public static IEnumerable<object[]> FuelsAndSplits =>
        DefaultFuels.All.SelectMany(fuel => Enumerable.Range(0, Splits.Length).Select(split => new object[] { fuel.Id, split }));

    [TestMethod]
    [DynamicData(nameof(FuelsAndSplits))]
    public void Burn_AnyFuelAndSplit_ConservesCarbon(string fuelId, int splitIndex)
    {
        var fuel = FuelById(fuelId);
        var split = Splits[splitIndex];
        const double FuelMass = 7;

        var stream = CombustionCalculator.Burn(fuel, FuelMass, split);
        var carbonIn = fuel.CarbonMassFraction * FuelMass;
        var fate = stream.CarbonByFate;

        TestAssert.AreWithin(carbonIn, fate.Total.TotalKg, 1e-12);
        TestAssert.AreWithin(carbonIn * split.Unburnt, fate.Unburnt.TotalKg, 1e-12);
        Assert.AreEqual(CarbonIn(stream.ProductMass(Material.CarbonDioxide), SpeciesCatalog.CarbonDioxide), fate.CarbonDioxide.TotalKg, 1e-12);
        Assert.AreEqual(CarbonIn(stream.ProductMass(Material.CarbonMonoxide), SpeciesCatalog.CarbonMonoxide), fate.CarbonMonoxide.TotalKg, 1e-12);
        Assert.AreEqual(stream.ProductMass(Material.Soot), fate.Soot.TotalKg, 1e-12);
    }

    [TestMethod]
    [DynamicData(nameof(FuelsAndSplits))]
    public void Burn_AnyFuelAndSplit_ConservesMass(string fuelId, int splitIndex)
    {
        var stream = CombustionCalculator.Burn(FuelById(fuelId), 7, Splits[splitIndex]);

        // NOx is a factor on top of the balance: it takes no oxygen from it (engine plan §6.1).
        var products = stream.Products.Where(amount => amount.Material != Material.Nox).Sum(amount => amount.MassKg);

        TestAssert.AreWithin(stream.Reactants.Sum(amount => amount.MassKg), products, 1e-12);
    }

    [TestMethod]
    [DynamicData(nameof(FuelsAndSplits))]
    public void Burn_AnyFuelAndSplit_DisplayReactionsAreBalanced(string fuelId, int splitIndex)
    {
        var stream = CombustionCalculator.Burn(FuelById(fuelId), 1, Splits[splitIndex]);

        Assert.IsNotEmpty(stream.Reactions);

        foreach (var reaction in stream.Reactions)
        {
            Assert.IsTrue(reaction.IsBalanced(), reaction.ToEquationString());
        }
    }

    [TestMethod]
    public void Burn_FuelOxygen_LowersOxygenDemand()
    {
        var withOxygen = DefaultFuels.BituminousCoal;
        var oxygenAsAsh = Fuel.CreateReal(
            "test",
            "test",
            withOxygen.Components.Select(component => component.Id switch
            {
                "oxygen" => component with { MassFraction = 0 },
                "ash" => component with { MassFraction = component.MassFraction + 0.07 },
                _ => component,
            }).ToList(),
            withOxygen.NetCalorificValue,
            null,
            0);

        var demandWithOxygen = CombustionCalculator.Burn(withOxygen, 1, CarbonSplit.Complete).ReactantMass(Material.Oxygen);
        var demandWithAsh = CombustionCalculator.Burn(oxygenAsAsh, 1, CarbonSplit.Complete).ReactantMass(Material.Oxygen);

        Assert.AreEqual(0.07, demandWithAsh - demandWithOxygen, 1e-12);
    }

    [TestMethod]
    public void Burn_OneKgIdealCoal_Gives3664KgCarbonDioxide()
    {
        var stream = CombustionCalculator.Burn(DefaultFuels.IdealCoal, 1, CarbonSplit.Complete);

        Assert.AreEqual(3.664, stream.ProductMass(Material.CarbonDioxide), 0.001);
    }

    [TestMethod]
    public void Burn_OneLitreRealPetrol_GivesAbout23KgCarbonDioxide()
    {
        var litre = DefaultFuels.Petrol.Density!.Value / 1000;

        var stream = CombustionCalculator.Burn(DefaultFuels.Petrol, litre, CarbonSplit.Complete);

        Assert.AreEqual(0.74, litre, 1e-12);
        Assert.AreEqual(2.3, stream.ProductMass(Material.CarbonDioxide), 0.05);
    }

    [TestMethod]
    public void Burn_OneKgIdealMethane_GivesCarbonDioxideAndWater()
    {
        var stream = CombustionCalculator.Burn(DefaultFuels.IdealNaturalGas, 1, CarbonSplit.Complete);

        Assert.AreEqual(2.75, stream.ProductMass(Material.CarbonDioxide), 0.01);
        Assert.AreEqual(2.25, stream.ProductMass(Material.Water), 0.01);
    }

    [TestMethod]
    public void Burn_CarbonToCarbonMonoxide_ReleasesAbout28PercentOfTheHeat()
    {
        var complete = CombustionCalculator.Burn(DefaultFuels.IdealCoal, 1, CarbonSplit.Complete).EnthalpyChange;
        var toCarbonMonoxide = CombustionCalculator.Burn(DefaultFuels.IdealCoal, 1, new CarbonSplit(0, 1, 0, 0)).EnthalpyChange;

        Assert.AreEqual(0.28, toCarbonMonoxide / complete, 0.005);
    }

    // Tab 1 plan §3: −802, −519, −408 and 0 kJ per mole of CH₄
    [TestMethod]
    [DataRow(1, 0, 0, 0, "combustion.methane.complete")]
    [DataRow(0, 1, 0, 0, "combustion.methane.carbonMonoxide")]
    [DataRow(0, 0, 1, 0, "combustion.methane.soot")]
    [DataRow(0, 0, 0, 1, null)]
    public void Burn_OneMoleIdealMethane_ReproducesIncompleteCombustionSeries(double carbonDioxide, double carbonMonoxide, double soot, double unburnt, string? reactionId)
    {
        var split = new CarbonSplit(carbonDioxide, carbonMonoxide, soot, unburnt);
        var oneMole = SpeciesCatalog.Methane.MolarMass / 1000;
        var expected = reactionId is null ? 0 : ReactionCatalog.All.Single(r => r.Id == reactionId).EnthalpyChange!.Value * 1000;

        var stream = CombustionCalculator.Burn(DefaultFuels.IdealNaturalGas, oneMole, split);

        Assert.AreEqual(expected, stream.EnthalpyChange, 1e-6);
        Assert.AreEqual(expected / 1000, stream.Reactions[0].EnthalpyChange!.Value, 1e-9);
    }

    [TestMethod]
    public void Burn_IdealMethaneWithSplit_GeneratesBalancedEquation()
    {
        var stream = CombustionCalculator.Burn(DefaultFuels.IdealNaturalGas, 1, new CarbonSplit(0, 1, 0, 0));

        Assert.AreEqual("2 CH₄ + 3 O₂ → 2 CO + 4 H₂O", stream.Reactions[0].ToEquationString());
    }

    [TestMethod]
    public void Burn_RealFuel_FirstReactionIsPerCarbonThenEachComponent()
    {
        var stream = CombustionCalculator.Burn(DefaultFuels.Petrol, 1, CarbonSplit.Complete);

        Assert.HasCount(3, stream.Reactions);
        Assert.AreEqual(1, stream.Reactions[0].Reactants[0].Coefficient);
        Assert.AreEqual(1, stream.Reactions[0].Reactants[0].Species.Formula.CountOf(Elements.C));
        Assert.IsTrue(stream.Reactions[1].Involves(SpeciesCatalog.Octane));
        Assert.IsTrue(stream.Reactions[2].Involves(SpeciesCatalog.Toluene));
    }

    [TestMethod]
    public void SulfurCombustion_OneKgSulfur_GivesAboutTwoKgSulfurDioxide()
    {
        var scaled = ReactionCatalog.SulfurCombustion.ScaleByMass(SpeciesCatalog.Sulfur, 1);

        Assert.AreEqual(2.0, scaled.Of(SpeciesCatalog.SulfurDioxide).MassKg, 0.01);
    }

    [TestMethod]
    public void Burn_FuelWithSulfur_AllSulfurLeavesAsSulfurDioxide()
    {
        var fuel = DefaultFuels.HeavyFuelOil;

        var stream = CombustionCalculator.Burn(fuel, 1000, CarbonSplit.Complete);

        var expected = 1000 * fuel.SulfurMassFraction * (SpeciesCatalog.SulfurDioxide.MolarMass / SpeciesCatalog.Sulfur.MolarMass);
        Assert.AreEqual(expected, stream.ProductMass(Material.SulfurDioxide), 1e-9);
    }

    [TestMethod]
    [DataRow("fuel.coal.ideal")]
    [DataRow("fuel.naturalGas.ideal")]
    [DataRow("fuel.petrol.ideal")]
    public void Burn_IdealFuel_GivesNoSulfurDioxide(string fuelId)
    {
        var stream = CombustionCalculator.Burn(FuelById(fuelId), 1, CarbonSplit.Complete);

        Assert.AreEqual(0, stream.ProductMass(Material.SulfurDioxide));
    }

    [TestMethod]
    public void Burn_UnburntNaturalGas_ReportsMethaneAndOtherComponentsAsNmvoc()
    {
        var stream = CombustionCalculator.Burn(DefaultFuels.NaturalGas, 1, new CarbonSplit(0.9, 0, 0, 0.1));

        Assert.AreEqual(0.1 * 0.9, stream.ProductMass(Material.UnburntMethane), 1e-12);
        Assert.AreEqual(0.1 * 0.1, stream.ProductMass(Material.Nmvoc), 1e-12);
    }

    [TestMethod]
    public void Burn_UnburntCoal_ReportsFuelMatterAsNmvocAndPassesMoistureAndAsh()
    {
        var coal = DefaultFuels.BituminousCoal;

        var stream = CombustionCalculator.Burn(coal, 1, new CarbonSplit(0, 0, 0, 1));

        Assert.AreEqual(1 - coal.MoistureMassFraction - coal.AshMassFraction, stream.ProductMass(Material.Nmvoc), 1e-12);
        Assert.AreEqual(coal.MoistureMassFraction, stream.ProductMass(Material.Water), 1e-12);
        Assert.AreEqual(coal.AshMassFraction, stream.ProductMass(Material.Ash), 1e-12);
        Assert.AreEqual(0, stream.EnthalpyChange);
        Assert.AreEqual(0, stream.ReactantMass(Material.Oxygen));
    }

    [TestMethod]
    public void Burn_Nox_IsFactorTimesFuelMassIndependentOfSplit()
    {
        var complete = CombustionCalculator.Burn(DefaultFuels.Diesel, 2, CarbonSplit.Complete);
        var smoky = CombustionCalculator.Burn(DefaultFuels.Diesel, 2, new CarbonSplit(0.9, 0.05, 0.05, 0));

        Assert.AreEqual(2 * DefaultFuels.Diesel.NoxFactor, complete.ProductMass(Material.Nox), 1e-15);
        Assert.AreEqual(complete.ProductMass(Material.Nox), smoky.ProductMass(Material.Nox), 1e-15);
    }

    [TestMethod]
    [DataRow("fuel.coal.ideal")]
    [DataRow("fuel.bituminousCoal")]
    [DataRow("fuel.petrol")]
    [DataRow("fuel.naturalGas")]
    public void Burn_OneKgAndTenToTheTwelveKg_AreInExactProportion(string fuelId)
    {
        var fuel = FuelById(fuelId);
        var split = new CarbonSplit(0.9, 0.05, 0.03, 0.02);
        const double Scale = 1e12;

        var small = CombustionCalculator.Burn(fuel, 1, split);
        var large = CombustionCalculator.Burn(fuel, Scale, split);

        TestAssert.AreWithin(small.EnthalpyChange * Scale, large.EnthalpyChange, 1e-12);
        Assert.HasCount(small.Products.Count, large.Products);

        foreach (var (smallAmount, largeAmount) in small.Products.Zip(large.Products))
        {
            Assert.AreEqual(smallAmount.Material, largeAmount.Material);
            TestAssert.AreWithin(smallAmount.MassKg * Scale, largeAmount.MassKg, 1e-12, smallAmount.Material.ToString());
        }
    }

    private static Fuel FuelById(string id) => DefaultFuels.All.Single(fuel => fuel.Id == id);

    private static double CarbonIn(double massKg, Species species) =>
        massKg * species.Formula.CountOf(Elements.C) * (Elements.C.StandardAtomicWeight / species.MolarMass);
}
