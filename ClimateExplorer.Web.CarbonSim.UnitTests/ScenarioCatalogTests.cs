namespace ClimateExplorer.Web.CarbonSim.UnitTests;

using System.Linq;
using ClimateExplorer.Web.Client.CarbonSim.Scenarios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ScenarioCatalogTests
{
    [TestMethod]
    public void All_EveryScenario_ShownByScenarioComponent()
    {
        foreach (var scenario in ScenarioCatalog.All)
        {
            Assert.IsTrue(scenario.Component.IsSubclassOf(typeof(ScenarioComponent)), scenario.Title);
        }
    }

    [TestMethod]
    public void All_Titles_Unique()
    {
        Assert.HasCount(ScenarioCatalog.All.Count, ScenarioCatalog.All.Select(x => x.Title).Distinct());
    }

    [TestMethod]
    public void All_FirstScenario_FillsFossilFuelsTab()
    {
        var fossilFuels = ScenarioCatalog.All[0].Inputs.FossilFuels;

        Assert.IsNotNull(fossilFuels);
        Assert.IsGreaterThan(0, fossilFuels.MethaneBurnerRate);
    }

    [TestMethod]
    public void All_BlankScenarios_NoInputs()
    {
        var blanks = ScenarioCatalog.All.Where(x => x.Component == typeof(BlankScenario)).ToList();

        Assert.HasCount(2, blanks);

        foreach (var blank in blanks)
        {
            Assert.AreEqual(ScenarioInputs.None, blank.Inputs);
        }
    }
}
