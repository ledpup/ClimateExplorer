namespace ClimateExplorer.Core.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TrendScoreTests
{
    private const int NewestLastYear = 2025;

    [TestMethod]
    public void SetTrendScores_EvenlySpacedRates_ScoresInEqualWidthSteps()
    {
        // 0.000 to 0.100 a year. The 1st and 99th percentiles are 0.001 and 0.099, so each step is 0.0098 wide.
        var locations = CreateEvenlySpacedLocations(101);

        Location.SetTrendScores(locations);

        Assert.AreEqual((short)0, locations[0].TrendScore);
        Assert.AreEqual((short)0, locations[1].TrendScore);
        Assert.AreEqual((short)1, locations[11].TrendScore);
        Assert.AreEqual((short)2, locations[21].TrendScore);
        Assert.AreEqual((short)5, locations[51].TrendScore);
        Assert.AreEqual((short)9, locations[99].TrendScore);
        Assert.AreEqual((short)9, locations[100].TrendScore);
    }

    [TestMethod]
    public void SetTrendScores_OneOutlier_ScoresNineWithoutStretchingTheSteps()
    {
        var locations = CreateEvenlySpacedLocations(101);
        var outlier = CreateLocation(since1961Slope: 0.5);
        locations.Add(outlier);

        Location.SetTrendScores(locations);

        Assert.AreEqual((short)9, outlier.TrendScore);
        Assert.AreEqual((short)1, locations[11].TrendScore);
        Assert.AreEqual((short)5, locations[51].TrendScore);
    }

    [TestMethod]
    public void SetTrendScores_RatesAndAnomaliesDisagree_RanksOnTheSince1961Rate()
    {
        var locations = Enumerable.Range(0, 10)
            .Select(i => CreateLocation(since1961Slope: 0.001 * i, warmingAnomaly: 2 - (0.1 * i)))
            .ToList();

        Location.SetTrendScores(locations);

        Assert.AreEqual((short)9, locations[9].TrendScore);
        Assert.AreEqual((short)0, locations[0].TrendScore);
    }

    [TestMethod]
    public void SetTrendScores_EvenlySpacedRates_ReturnsEqualWidthBandsPerDecade()
    {
        var table = Location.SetTrendScores(CreateEvenlySpacedLocations(101));

        Assert.HasCount(10, table);
        CollectionAssert.AreEqual(Enumerable.Range(0, 10).Reverse().ToList(), table.Select(x => x.Score).ToList());

        // 101 evenly spaced rates, so about ten to a step; the ends also hold the rates beyond the percentiles.
        Assert.AreEqual(101, table.Sum(x => x.LocationCount));
        Assert.IsTrue(table.All(x => x.LocationCount is >= 9 and <= 11));

        // The top and bottom bands run out to the actual highest and lowest rates.
        Assert.AreEqual(1.0, table[0].MaximumRatePerDecade!.Value, 1e-9);
        Assert.AreEqual(0.892, table[0].MinimumRatePerDecade!.Value, 1e-9);
        Assert.AreEqual(0.108, table[^1].MaximumRatePerDecade!.Value, 1e-9);
        Assert.AreEqual(0, table[^1].MinimumRatePerDecade!.Value, 1e-9);

        // The steps in between are all 0.098 °C per decade wide and touch.
        for (var i = 1; i < 9; i++)
        {
            Assert.AreEqual(0.098, table[i].MaximumRatePerDecade!.Value - table[i].MinimumRatePerDecade!.Value, 1e-9);
            Assert.AreEqual(table[i].MinimumRatePerDecade!.Value, table[i + 1].MaximumRatePerDecade!.Value, 1e-9);
        }
    }

    [TestMethod]
    public void SetTrendScores_NoSpreadInRates_ScoresZeroWithOneBand()
    {
        var locations = new List<Location> { CreateLocation(since1961Slope: 0.03), CreateLocation(since1961Slope: 0.03) };

        var table = Location.SetTrendScores(locations);

        Assert.IsTrue(locations.All(x => x.TrendScore == 0));
        Assert.HasCount(1, table);
        Assert.AreEqual(2, table[0].LocationCount);
        Assert.AreEqual(0.3, table[0].MinimumRatePerDecade!.Value, 1e-9);
        Assert.AreEqual(0.3, table[0].MaximumRatePerDecade!.Value, 1e-9);
    }

    [TestMethod]
    public void GetTrendScoreTable_AnyLocations_HasTheScoreRowsWithoutSettingScores()
    {
        var locations = CreateEvenlySpacedLocations(101);

        var table = Location.GetTrendScoreTable(locations);

        Assert.IsTrue(locations.All(x => x.TrendScore is null));
        CollectionAssert.AreEqual(Location.SetTrendScores(locations), table.Take(10).ToList());
    }

    [TestMethod]
    public void GetTrendScoreTable_LocationsWithoutAScore_EndsWithANoScoreRowCountingThem()
    {
        var locations = CreateEvenlySpacedLocations(101);
        locations.Add(CreateLocation(since1961Slope: null));
        locations.Add(CreateLocation(since1961Slope: 0.05, lastYear: NewestLastYear - Location.TrendScoreMaximumYearsBehindNewest - 1));

        var table = Location.GetTrendScoreTable(locations);

        Assert.HasCount(12, table);
        Assert.IsNull(table[^1].Score);
        Assert.IsNull(table[^1].MinimumRatePerDecade);
        Assert.IsNull(table[^1].MaximumRatePerDecade);
        Assert.AreEqual(2, table[^1].LocationCount);
        Assert.AreEqual(101, table.Take(10).Sum(x => x.LocationCount));
    }

    [TestMethod]
    public void GetTrendScoreTable_NoLocationHasAScore_IsJustTheNoScoreRow()
    {
        var table = Location.GetTrendScoreTable([CreateLocation(since1961Slope: null)]);

        Assert.HasCount(2, table);
        Assert.AreEqual(0, table[0].LocationCount);
        Assert.IsNull(table[1].Score);
        Assert.AreEqual(1, table[1].LocationCount);
    }

    [TestMethod]
    public void GetTrendScoreTable_NegativeRates_HaveARowWithTheirRangeAndCount()
    {
        var locations = CreateEvenlySpacedLocations(101);
        locations.Add(CreateLocation(since1961Slope: -0.006));
        locations.Add(CreateLocation(since1961Slope: -0.0001));

        var table = Location.GetTrendScoreTable(locations);

        var negative = table[^2];
        Assert.AreEqual(Location.NegativeRateTrendScore, negative.Score);
        Assert.AreEqual(2, negative.LocationCount);
        Assert.AreEqual(-0.06, negative.MinimumRatePerDecade!.Value, 1e-9);
        Assert.AreEqual(-0.001, negative.MaximumRatePerDecade!.Value, 1e-9);
        Assert.AreEqual(101, table.Take(10).Sum(x => x.LocationCount));
    }

    [TestMethod]
    public void SetTrendScores_NegativeRate_ScoresMinusOne()
    {
        var locations = new List<Location> { CreateLocation(since1961Slope: -0.006), CreateLocation(since1961Slope: -0.0001) };

        var table = Location.SetTrendScores(locations);

        Assert.IsTrue(locations.All(x => x.TrendScore == Location.NegativeRateTrendScore));
        Assert.IsEmpty(table);
    }

    [TestMethod]
    public void SetTrendScores_NoSince1961Trend_HasNoScore()
    {
        var location = CreateLocation(since1961Slope: null, warmingAnomaly: 1.5);
        location.TrendScore = 7;

        Location.SetTrendScores([location, CreateLocation(since1961Slope: 0.02)]);

        Assert.IsNull(location.TrendScore);
    }

    [TestMethod]
    public void SetTrendScores_LastYearMoreThanTenYearsBehindNewest_HasNoScore()
    {
        var stale = CreateLocation(since1961Slope: 0.05, lastYear: NewestLastYear - Location.TrendScoreMaximumYearsBehindNewest - 1);

        Location.SetTrendScores([stale, CreateLocation(since1961Slope: 0.02)]);

        Assert.IsNull(stale.TrendScore);
    }

    [TestMethod]
    public void SetTrendScores_LastYearTenYearsBehindNewest_IsScored()
    {
        var older = CreateLocation(since1961Slope: 0.05, lastYear: NewestLastYear - Location.TrendScoreMaximumYearsBehindNewest);

        Location.SetTrendScores([older, CreateLocation(since1961Slope: 0.02)]);

        Assert.IsNotNull(older.TrendScore);
    }

    [TestMethod]
    public void SetTrendScores_NoLocations_ReturnsEmptyTable()
    {
        Assert.IsEmpty(Location.SetTrendScores([]));
    }

    [TestMethod]
    public void TrendScoreRow_JsonWithoutNullProperties_Deserializes()
    {
        // The API leaves nulls out, so the no-score row arrives as only its count.
        var row = JsonSerializer.Deserialize<TrendScoreRow>("""{"locationCount":1582}""", JsonSerializerOptions.Web);

        Assert.IsNotNull(row);
        Assert.IsNull(row.Score);
        Assert.IsNull(row.MinimumRatePerDecade);
        Assert.IsNull(row.MaximumRatePerDecade);
        Assert.AreEqual(1582, row.LocationCount);
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
