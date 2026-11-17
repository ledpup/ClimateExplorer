namespace ClimateExplorer.WebApi.UnitTests;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MetadataEndpointsTrendScoreTableTests
{
    [TestMethod]
    public async Task GetTrendScoreTable_CalledTwice_ReturnsTheMemoisedTable()
    {
        var services = CreateServices();
        services.Locations = CreateLocations(20);

        var first = await MetadataEndpoints.GetTrendScoreTable(services);
        var second = await MetadataEndpoints.GetTrendScoreTable(services);

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public async Task GetTrendScoreTable_LocationsReplaced_RebuildsTheTable()
    {
        var services = CreateServices();
        services.Locations = CreateLocations(20);
        var first = await MetadataEndpoints.GetTrendScoreTable(services);

        services.Locations = CreateLocations(30);
        var second = await MetadataEndpoints.GetTrendScoreTable(services);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(30, second.Sum(x => x.LocationCount));
    }

    // No caches: the tests set Locations, so GetCachedLocations returns them without a cache lookup.
    private static ClimateExplorerApiServices CreateServices() =>
        new(null!, null!, new HttpClient());

    private static Location[] CreateLocations(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new Location
            {
                Id = Guid.NewGuid(),
                Name = "Unknown",
                Coordinates = new Coordinates(),
                CountryCode = "UU",
                LastYear = 2025,
                TemperatureTrends =
                [
                    new TrendSummary(TrendWindow.Full, 1900, 0.01, true),
                    new TrendSummary(TrendWindow.Since1961, 1961, 0.001 * (i + 1), true),
                ],
            })
            .ToArray();
}
