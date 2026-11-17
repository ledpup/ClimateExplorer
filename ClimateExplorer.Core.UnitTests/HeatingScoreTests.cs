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
    public void SetHeatingScores_EvenlySpacedRates_ScoresInEqualWidthSteps()
    {
        // 0.000 to 0.100 a year. The 1st and 99th percentiles are 0.001 and 0.099, so each step is 0.0098 wide.
        var locations = CreateEvenlySpacedLocations(101);

        Location.SetHeatingScores(locations);

        Assert.AreEqual((short)0, locations[0].HeatingScore);
        Assert.AreEqual((short)0, locations[1].HeatingScore);
        Assert.AreEqual((short)1, locations[11].HeatingScore);
        Assert.AreEqual((short)2, locations[21].HeatingScore);
        Assert.AreEqual((short)5, locations[51].HeatingScore);
        Assert.AreEqual((short)9, locations[99].HeatingScore);
        Assert.AreEqual((short)9, locations[100].HeatingScore);
    }

    [TestMethod]
    public void SetHeatingScores_OneOutlier_ScoresNineWithoutStretchingTheSteps()
    {
        var locations = CreateEvenlySpacedLocations(101);
        var outlier = CreateLocation(since1961Slope: 0.5);
        locations.Add(outlier);

        Location.SetHeatingScores(locations);

        Assert.AreEqual((short)9, outlier.HeatingScore);
        Assert.AreEqual((short)1, locations[11].HeatingScore);
        Assert.AreEqual((short)5, locations[51].HeatingScore);
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
    public void SetHeatingScores_EvenlySpacedRates_ReturnsEqualWidthBandsPerDecade()
    {
        var table = Location.SetHeatingScores(CreateEvenlySpacedLocations(101));

        Assert.HasCount(10, table);
        CollectionAssert.AreEqual(Enumerable.Range(0, 10).Reverse().ToList(), table.Select(x => x.Score).ToList());

        // The top and bottom bands run out to the actual highest and lowest rates.
        Assert.AreEqual(1.0, table[0].MaximumRatePerDecade, 1e-9);
        Assert.AreEqual(0.892, table[0].MinimumRatePerDecade, 1e-9);
        Assert.AreEqual(0.108, table[^1].MaximumRatePerDecade, 1e-9);
        Assert.AreEqual(0, table[^1].MinimumRatePerDecade, 1e-9);

        // The steps in between are all 0.098 °C per decade wide and touch.
        for (var i = 1; i < 9; i++)
        {
            Assert.AreEqual(0.098, table[i].MaximumRatePerDecade - table[i].MinimumRatePerDecade, 1e-9);
            Assert.AreEqual(table[i].MinimumRatePerDecade, table[i + 1].MaximumRatePerDecade, 1e-9);
        }
    }

    [TestMethod]
    public void SetHeatingScores_NoSpreadInRates_ScoresZeroWithOneBand()
    {
        var locations = new List<Location> { CreateLocation(since1961Slope: 0.03), CreateLocation(since1961Slope: 0.03) };

        var table = Location.SetHeatingScores(locations);

        Assert.IsTrue(locations.All(x => x.HeatingScore == 0));
        Assert.HasCount(1, table);
        Assert.AreEqual(0.3, table[0].MinimumRatePerDecade, 1e-9);
        Assert.AreEqual(0.3, table[0].MaximumRatePerDecade, 1e-9);
    }

    [TestMethod]
    public void GetHeatingScoreTable_AnyLocations_MatchesSetHeatingScoresWithoutSettingScores()
    {
        var locations = CreateEvenlySpacedLocations(101);

        var table = Location.GetHeatingScoreTable(locations);

        Assert.IsTrue(locations.All(x => x.HeatingScore is null));
        CollectionAssert.AreEqual(Location.SetHeatingScores(locations), table);
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

    private static List<Location> CreateEvenlySpacedLocations(int count) =>
        Enumerable.Range(0, count).Select(i => CreateLocation(since1961Slope: 0.001 * i)).ToList();

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
