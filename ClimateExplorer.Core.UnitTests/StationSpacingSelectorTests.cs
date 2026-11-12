using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.UnitTests;

[TestClass]
public class StationSpacingSelectorTests
{
    // At the equator, 0.1 degrees of longitude is about 11.1 km.
    private const double TenthDegreeKm = 11.1;

    [TestMethod]
    public void Select_CandidateWithinRadiusOfKeptStation_SkipsCandidate()
    {
        var better = new TestStation("better", "ES", 0, 0);
        var worse = new TestStation("worse", "ES", 0, 0.1);

        var selected = Select([], [better, worse], radiusKm: 15);

        CollectionAssert.AreEqual(new[] { "better" }, Ids(selected));
    }

    [TestMethod]
    public void Select_CandidateOutsideRadius_KeepsBoth()
    {
        var first = new TestStation("first", "ES", 0, 0);
        var second = new TestStation("second", "ES", 0, 0.1);

        var selected = Select([], [first, second], radiusKm: TenthDegreeKm - 1);

        CollectionAssert.AreEqual(new[] { "first", "second" }, Ids(selected));
    }

    [TestMethod]
    public void Select_CloseStationInAnotherCountry_KeepsBoth()
    {
        var spain = new TestStation("spain", "ES", 0, 0);
        var portugal = new TestStation("portugal", "PT", 0, 0.1);

        var selected = Select([], [spain, portugal], radiusKm: 25);

        CollectionAssert.AreEqual(new[] { "spain", "portugal" }, Ids(selected));
    }

    [TestMethod]
    public void Select_OneGroupForAllCountries_BlocksAcrossBorders()
    {
        // The GHCNm importer's isolation pass puts every station in one group, so a seed in another country blocks.
        var seed = new TestStation("seed", "ES", 0, 0);
        var acrossBorder = new TestStation("acrossBorder", "PT", 0, 0.5);
        var remote = new TestStation("remote", "PT", 0, 1);

        var selected = StationSpacingSelector.Select(
            [seed],
            [acrossBorder, remote],
            _ => string.Empty,
            x => new Coordinates(x.Latitude, x.Longitude),
            _ => 75);

        CollectionAssert.AreEqual(new[] { "remote" }, Ids(selected));
    }

    [TestMethod]
    public void Select_SeedWithinRadius_SkipsCandidateAndDoesNotReturnSeed()
    {
        var seed = new TestStation("seed", "AU", 0, 0);
        var near = new TestStation("near", "AU", 0, 0.1);
        var far = new TestStation("far", "AU", 0, 1);

        var selected = Select([seed], [near, far], radiusKm: 25);

        CollectionAssert.AreEqual(new[] { "far" }, Ids(selected));
    }

    [TestMethod]
    public void Select_ChainOfStations_KeepsEndsAndDropsOnlyTheMiddle()
    {
        // a-b and b-c are 11 km apart, a-c 22 km. DBSCAN joined all three into one cluster and kept one.
        var a = new TestStation("a", "ES", 0, 0);
        var b = new TestStation("b", "ES", 0, 0.1);
        var c = new TestStation("c", "ES", 0, 0.2);

        var selected = Select([], [a, c, b], radiusKm: 15);

        CollectionAssert.AreEqual(new[] { "a", "c" }, Ids(selected));
    }

    [TestMethod]
    public void Select_RadiusPerCandidate_UsesTheCandidatesOwnRadius()
    {
        var kept = new TestStation("kept", "ES", 0, 0, RadiusKm: 50);
        var longRecord = new TestStation("longRecord", "ES", 0, 0.2, RadiusKm: 10);
        var shortRecord = new TestStation("shortRecord", "ES", 0, -0.2, RadiusKm: 25);

        var selected = StationSpacingSelector.Select(
            [],
            [kept, longRecord, shortRecord],
            x => x.Group,
            x => new Coordinates(x.Latitude, x.Longitude),
            x => x.RadiusKm);

        CollectionAssert.AreEqual(new[] { "kept", "longRecord" }, Ids(selected));
    }

    private static List<TestStation> Select(TestStation[] seeds, TestStation[] candidates, double radiusKm)
    {
        return StationSpacingSelector.Select(
            seeds,
            candidates,
            x => x.Group,
            x => new Coordinates(x.Latitude, x.Longitude),
            _ => radiusKm);
    }

    private static string[] Ids(IEnumerable<TestStation> stations) => [.. stations.Select(x => x.Id)];

    private sealed record TestStation(string Id, string Group, double Latitude, double Longitude, double RadiusKm = 0);
}
