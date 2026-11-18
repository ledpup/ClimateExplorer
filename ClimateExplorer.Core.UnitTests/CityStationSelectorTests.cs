using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.UnitTests;

[TestClass]
public class CityStationSelectorTests
{
    // At the equator, 0.1 degrees of longitude is about 11.1 km.
    private const double CoveredRadiusKm = 30;
    private const double CandidateRadiusKm = 25;
    private const double MinimumDistanceToKeptKm = 10;

    [TestMethod]
    public void Select_KeptStationWithinCoveredRadius_SkipsCity()
    {
        var city = new City("Covered", "ES", new Coordinates(0, 0), 100_000);
        var seed = new TestStation("seed", "ES", 0, 0.2);
        var candidate = new TestStation("candidate", "ES", 0, -0.05);

        var selected = Select([city], [seed], [candidate]);

        Assert.IsEmpty(selected);
    }

    [TestMethod]
    public void Select_CandidateBeyondCandidateRadius_DoesNotPickIt()
    {
        var city = new City("City", "ES", new Coordinates(0, 0), 100_000);
        var tooFar = new TestStation("tooFar", "ES", 0, 0.3);

        var selected = Select([city], [], [tooFar]);

        Assert.IsEmpty(selected);
    }

    [TestMethod]
    public void Select_CandidateWithinMinimumDistanceOfKeptStation_PicksTheNextCandidate()
    {
        // The seed is 33 km from the city, so the city isn't covered. The first candidate is 24 km from the city
        // but 9 km from the seed, which already serves that side of town.
        var city = new City("City", "ES", new Coordinates(0, 0), 100_000);
        var seed = new TestStation("seed", "ES", 0, 0.3);
        var nearSeed = new TestStation("nearSeed", "ES", 0, 0.22);
        var otherSide = new TestStation("otherSide", "ES", 0, -0.1);

        var selected = Select([city], [seed], [nearSeed, otherSide]);

        CollectionAssert.AreEqual(new[] { "otherSide" }, Ids(selected));
    }

    [TestMethod]
    public void Select_TwoEligibleCandidates_PicksTheFirstInPriorityOrder()
    {
        var city = new City("City", "ES", new Coordinates(0, 0), 100_000);
        var betterRecord = new TestStation("betterRecord", "ES", 0, 0.15);
        var nearer = new TestStation("nearer", "ES", 0, 0.01);

        var selected = Select([city], [], [betterRecord, nearer]);

        CollectionAssert.AreEqual(new[] { "betterRecord" }, Ids(selected));
        Assert.AreEqual(16.7, selected.Single().DistanceKm, 0.1);
    }

    [TestMethod]
    public void Select_PickedStation_CoversSmallerCityNearIt()
    {
        var big = new City("Big", "ES", new Coordinates(0, 0), 200_000);
        var small = new City("Small", "ES", new Coordinates(0, 0.2), 60_000);
        var between = new TestStation("between", "ES", 0, 0.08);
        var nearSmall = new TestStation("nearSmall", "ES", 0, 0.25);

        // Listed smallest first: the selector still takes the largest city first.
        var selected = Select([small, big], [], [between, nearSmall]);

        Assert.HasCount(1, selected);
        Assert.AreEqual("Big", selected[0].City.Name);
        Assert.AreEqual("between", selected[0].Station.Id);
    }

    [TestMethod]
    public void Select_Geelong_MeasuresFromTheCityNotTheStation()
    {
        // Laverton (Melbourne's station) is 48 km from Geelong but 31 km from Avalon Airport, 17 km from Geelong.
        // The isolation rule (75 km from the station) blocks Avalon; measured from the city centre, Geelong has nothing.
        var geelong = new City("Geelong", "AU", new Coordinates(-38.14711, 144.36069), 282_809);
        var laverton = new TestStation("Laverton", "AU", -37.86, 144.76);
        var avalon = new TestStation("ASN00087113", "AU", -38.0287, 144.4783);

        var selected = Select([geelong], [laverton], [avalon]);

        Assert.HasCount(1, selected);
        Assert.AreEqual("Geelong", selected[0].City.Name);
        Assert.AreEqual("ASN00087113", selected[0].Station.Id);
        Assert.AreEqual(16.7, selected[0].DistanceKm, 0.2);
    }

    [TestMethod]
    public void Select_StationInAnotherCountry_NeitherCoversNorIsPicked()
    {
        var city = new City("Tijuana", "MX", new Coordinates(0, 0), 1_000_000);
        var seedAcrossBorder = new TestStation("seedAcrossBorder", "US", 0, 0.05);
        var candidateAcrossBorder = new TestStation("candidateAcrossBorder", "US", 0, 0.1);
        var sameCountry = new TestStation("sameCountry", "MX", 0, -0.15);

        var selected = Select([city], [seedAcrossBorder], [candidateAcrossBorder, sameCountry]);

        CollectionAssert.AreEqual(new[] { "sameCountry" }, Ids(selected));
    }

    [TestMethod]
    public void Select_CandidateNearerABiggerCity_IsNotPickedForTheSmallerCity()
    {
        // The big city is covered by the seed. The first candidate is 20 km from the big city's centre and 24 km
        // from the small city's, so it's the big city's station, not the small city's.
        var big = new City("Fort Lauderdale", "US", new Coordinates(0, 0), 180_000);
        var small = new City("Boca Raton", "US", new Coordinates(0, 0.4), 98_000);
        var seed = new TestStation("seed", "US", 0, -0.1);
        var nearerBig = new TestStation("nearerBig", "US", 0, 0.18);
        var nearerSmall = new TestStation("nearerSmall", "US", 0, 0.5);

        var selected = Select([big, small], [seed], [nearerBig, nearerSmall]);

        Assert.HasCount(1, selected);
        Assert.AreEqual("Boca Raton", selected[0].City.Name);
        Assert.AreEqual("nearerSmall", selected[0].Station.Id);
    }

    [TestMethod]
    public void Select_CandidateNearerASmallerCity_CanBePickedForTheBiggerCity()
    {
        // Only bigger cities count: a station is a big city's airport even when a smaller town is closer.
        var big = new City("Johannesburg", "ZA", new Coordinates(0, 0), 2_000_000);
        var small = new City("Kempton Park", "ZA", new Coordinates(0, 0.25), 170_000);
        var airport = new TestStation("airport", "ZA", 0, 0.2);

        var selected = Select([big, small], [], [airport]);

        Assert.HasCount(1, selected);
        Assert.AreEqual("Johannesburg", selected[0].City.Name);
    }

    private static List<CityStation<TestStation>> Select(City[] cities, TestStation[] seeds, TestStation[] candidates)
    {
        return CityStationSelector.Select(
            cities,
            seeds,
            candidates,
            x => x.CountryCode,
            x => new Coordinates(x.Latitude, x.Longitude),
            CoveredRadiusKm,
            CandidateRadiusKm,
            MinimumDistanceToKeptKm);
    }

    private static string[] Ids(IEnumerable<CityStation<TestStation>> selected) => [.. selected.Select(x => x.Station.Id)];

    private sealed record TestStation(string Id, string CountryCode, double Latitude, double Longitude);
}
