using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class HeatingScoreTests
{
    [TestMethod]
    public void CategoriseStandardScores()
    {
        var locations = new List<Location>();
        for (int i = 0; i < 20; i++) {
            locations.Add(new Location
            {
                Id = Guid.Empty,
                Name = "Unknown",
                Coordinates = new Coordinates(),
                CountryCode = "UU",
                WarmingAnomaly = .1f * i,
            });
        }

        Location.SetHeatingScores(locations);

        for (short i = 0; i < 10; i++)
        {
            Assert.AreEqual(2, locations.Count(x => x.HeatingScore == i));
        }
        Assert.IsTrue(!locations.Any(x => x.HeatingScore > 9));
        Assert.IsTrue(!locations.Any(x => x.HeatingScore < 0));
    }

    [TestMethod]
    public void NullWarmingAnomaly()
    {
        var locations = new List<Location>
        {
            new()
            {
                Id = Guid.Empty,
                Name = "Unknown",
                Coordinates = new Coordinates(),
                CountryCode = "UU",
                WarmingAnomaly = null,
            }
        };

        Location.SetHeatingScores(locations);

        Assert.IsTrue(locations.All(x => x.HeatingScore == null));
    }

    [TestMethod]
    public void NegativeWarmingAnomaly()
    {
        var locations = new List<Location>
        {
            new() {
                Id = Guid.Empty,
                Name = "Unknown",
                Coordinates = new Coordinates(),
                CountryCode = "UU",
                WarmingAnomaly = -.6d,
            }
        };

        Location.SetHeatingScores(locations);

        Assert.IsTrue(locations.All(x => x.HeatingScore == -1));
    }
}
