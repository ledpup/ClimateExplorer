using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.CarbonCycle.Chemistry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class MechanismTests
{
    public static IEnumerable<object[]> Mechanisms => MechanismCatalog.All.Select(mechanism => new object[] { mechanism.Id });

    [TestMethod]
    [DynamicData(nameof(Mechanisms))]
    public void StepsSumToNet_CatalogMechanism_IsTrue(string id)
    {
        var mechanism = MechanismCatalog.All.Single(m => m.Id == id);

        Assert.IsTrue(mechanism.StepsSumToNet(), $"{mechanism.SumOfSteps().ToEquationString()} vs {mechanism.Net.ToEquationString()}");
    }

    [TestMethod]
    public void SumOfSteps_CarbonMonoxideOzone_RegeneratesRadicalsAndNox()
    {
        var sum = MechanismCatalog.CarbonMonoxideOzone.SumOfSteps();

        Assert.AreEqual("CO + 2 O₂ → CO₂ + O₃", sum.ToEquationString());
    }

    [TestMethod]
    public void StepsSumToNet_WrongNet_IsFalse()
    {
        var mechanism = new Mechanism("test", "test", MechanismCatalog.ZeldovichNitricOxide.Steps, ReactionCatalog.NitricOxideOxidation);

        Assert.IsFalse(mechanism.StepsSumToNet());
    }
}
