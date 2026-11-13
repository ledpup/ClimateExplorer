namespace ClimateExplorer.Core.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class HeatingScoreTests
{
    private const int NewestLastYear = 2025;

    [TestMethod]
    public void SetHeatingScores_TwentyPositiveRates_TwoLocationsPerScore()
    {
        var locations = Enumerable.Range(0, 20).Select(i => CreateLocation(since1961Slope: 0.001 * i)).ToList();

        Location.SetHeatingScores(locations);

        for (short score = 0; score < 10; score++)
        {
            Assert.AreEqual(2, locations.Count(x => x.HeatingScore == score));
        }

        Assert.AreEqual((short)9, locations[19].HeatingScore);
        Assert.AreEqual((short)0, locations[0].HeatingScore);
    }

    [TestMethod]
    public void SetHeatingScores_RatesAndAnomaliesDisagree_RanksOnTheSince1961Rate()
    {
        var locations = Enumerable.Range(0, 10)
            .Select(i => CreateLocation(since1961Slope: 0.001 * i, warmingAnomaly: 2 - (0.1 * i)))
            .ToList();

        Location.SetHeatingScores(locations);

        Assert.AreEqual((short)9, locations[9].HeatingScore);
        Assert.AreEqual((short)0, locations[0].HeatingScore);
    }

    [TestMethod]
    public void SetHeatingScores_TwentyPositiveRates_ReturnsBandsPerDecade()
    {
        var locations = Enumerable.Range(0, 20).Select(i => CreateLocation(since1961Slope: 0.001 * i)).ToList();

        var table = Location.SetHeatingScores(locations);

        Assert.HasCount(10, table);
        Assert.AreEqual(9, table[0].Score);
        Assert.AreEqual(0.19, table[0].MaximumRatePerDecade, 1e-12);
        Assert.AreEqual(0.18, table[0].MinimumRatePerDecade, 1e-12);
        Assert.AreEqual(0, table[^1].Score);
        Assert.AreEqual(0.01, table[^1].MaximumRatePerDecade, 1e-12);
        Assert.AreEqual(0, table[^1].MinimumRatePerDecade, 1e-12);
    }

    [TestMethod]
    public void SetHeatingScores_NegativeRate_ScoresMinusOne()
    {
        var locations = new List<Location> { CreateLocation(since1961Slope: -0.006), CreateLocation(since1961Slope: -0.0001) };

        var table = Location.SetHeatingScores(locations);

        Assert.IsTrue(locations.All(x => x.HeatingScore == Location.NegativeRateHeatingScore));
        Assert.IsEmpty(table);
    }

    [TestMethod]
    public void SetHeatingScores_NoSince1961Trend_HasNoScore()
    {
        var location = CreateLocation(since1961Slope: null, warmingAnomaly: 1.5);
        location.HeatingScore = 7;

        Location.SetHeatingScores([location, CreateLocation(since1961Slope: 0.02)]);

        Assert.IsNull(location.HeatingScore);
    }

    [TestMethod]
    public void SetHeatingScores_LastYearMoreThanTenYearsBehindNewest_HasNoScore()
    {
        var stale = CreateLocation(since1961Slope: 0.05, lastYear: NewestLastYear - Location.HeatingScoreMaximumYearsBehindNewest - 1);

        Location.SetHeatingScores([stale, CreateLocation(since1961Slope: 0.02)]);

        Assert.IsNull(stale.HeatingScore);
    }

    [TestMethod]
    public void SetHeatingScores_LastYearTenYearsBehindNewest_IsScored()
    {
        var older = CreateLocation(since1961Slope: 0.05, lastYear: NewestLastYear - Location.HeatingScoreMaximumYearsBehindNewest);

        Location.SetHeatingScores([older, CreateLocation(since1961Slope: 0.02)]);

        Assert.IsNotNull(older.HeatingScore);
    }

    [TestMethod]
    public void SetHeatingScores_NoLocations_ReturnsEmptyTable()
    {
        Assert.IsEmpty(Location.SetHeatingScores([]));
    }

    private static Location CreateLocation(double? since1961Slope, int lastYear = NewestLastYear, double? warmingAnomaly = null)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Unknown",
            Coordinates = new Coordinates(),
            CountryCode = "UU",
            WarmingAnomaly = warmingAnomaly,
            LastYear = lastYear,
            TemperatureTrends = [new TrendSummary(TrendWindow.Full, 1900, 0.01, true)],
        };

        if (since1961Slope is { } slope)
        {
            location.TemperatureTrends.Add(new TrendSummary(TrendWindow.Since1961, 1961, slope, true));
        }

        return location;
    }
}
