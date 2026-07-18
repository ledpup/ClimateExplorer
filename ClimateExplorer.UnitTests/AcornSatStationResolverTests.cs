namespace ClimateExplorer.UnitTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi.AcornSat;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class AcornSatStationResolverTests
{
    // Adelaide: adjusted station 023000 throughout; CDO closes 023000 in 1977, opens 023090 until
    // 2018-06-30, then reopens 023000. The open (null EndDate) CDO station is 023000 - the same as adjusted.
    private static readonly Guid AdelaideLocationId = Guid.Parse("70a07bb0-2220-402b-be83-f2c35edfdd12");

    // The all-null placeholder location present in both Australian mappings.
    private static readonly Guid AllNullPlaceholderLocationId = Guid.Parse("143983a0-240e-447f-8578-8daf2c0a246a");

    [TestMethod]
    public async Task Resolve_AdelaideCloseReopenHistory_ResolvesMatchingAdjustedAndOpenCdoStations()
    {
        var (acornSat, cdo) = await GetAcornSatAndCdoDataSetDefinitions();

        var result = AcornSatStationResolver.Resolve(acornSat, cdo, AdelaideLocationId);

        Assert.AreEqual("023000", result.AdjustedStationId);
        Assert.AreEqual("023000", result.OpenCdoStationId);
        Assert.IsTrue(result.IsResolved);
    }

    [TestMethod]
    public async Task Resolve_AllNullPlaceholderLocation_ReturnsUnresolved()
    {
        var (acornSat, cdo) = await GetAcornSatAndCdoDataSetDefinitions();

        var result = AcornSatStationResolver.Resolve(acornSat, cdo, AllNullPlaceholderLocationId);

        Assert.IsNull(result.AdjustedStationId);
        Assert.IsFalse(result.IsResolved);
    }

    [TestMethod]
    public async Task Resolve_UnknownLocation_ReturnsUnresolved()
    {
        var (acornSat, cdo) = await GetAcornSatAndCdoDataSetDefinitions();

        var result = AcornSatStationResolver.Resolve(acornSat, cdo, Guid.NewGuid());

        Assert.IsFalse(result.IsResolved);
    }

    [TestMethod]
    public async Task Resolve_AllRealAcornSatLocations_ResolveToExactlyOneOpenCdoStation()
    {
        var (acornSat, cdo) = await GetAcornSatAndCdoDataSetDefinitions();

        var realLocationIds = acornSat.DataLocationMapping!.LocationIdToDataFileMappings
            .Where(x => x.Value.Any(f => !string.IsNullOrWhiteSpace(f.Id)))
            .Select(x => x.Key);

        foreach (var locationId in realLocationIds)
        {
            var result = AcornSatStationResolver.Resolve(acornSat, cdo, locationId);
            Assert.IsTrue(result.IsResolved, $"Location {locationId} should resolve to exactly one adjusted station and one open CDO station.");
        }
    }

    private static async Task<(DataSetDefinition AcornSat, DataSetDefinition Cdo)> GetAcornSatAndCdoDataSetDefinitions()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions();
        return (
            definitions.Single(x => x.ShortName == "ACORN-SAT"),
            definitions.Single(x => x.ShortName == "CDO"));
    }
}
