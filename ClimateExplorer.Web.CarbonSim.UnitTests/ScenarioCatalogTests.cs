namespace ClimateExplorer.Web.CarbonSim.UnitTests;

using System.Linq;
using ClimateExplorer.CarbonCycle.Simulation;
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
    public void All_SecondScenario_BillionCarsOnADeadPlanet()
    {
        var scenario = ScenarioCatalog.All[1];

        var planet = scenario.CreateSimulation(scenario.Inputs);

        Assert.AreEqual(typeof(DeadPlanetScenario), scenario.Component);
        Assert.IsInstanceOfType<DeadPlanet>(planet);
        Assert.AreEqual(1e9, ((DeadPlanet)planet).Settings.Cars);
    }

    [TestMethod]
    public void All_EveryScenario_StartsWithItsOwnInputs()
    {
        foreach (var scenario in ScenarioCatalog.All)
        {
            var start = scenario.CreateSimulation(scenario.Inputs).Start();

            Assert.AreEqual(scenario.Inputs.FossilFuels is not null, start.FlameOn, scenario.Title);
        }
    }

    [TestMethod]
    public void All_BlankScenarios_NoInputs()
    {
        var blanks = ScenarioCatalog.All.Where(x => x.Component == typeof(BlankScenario)).ToList();

        Assert.HasCount(1, blanks);

        foreach (var blank in blanks)
        {
            Assert.AreEqual(ScenarioInputs.None, blank.Inputs);
        }
    }
}
