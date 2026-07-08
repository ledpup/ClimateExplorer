namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class LocationDataSetMetadataServiceTests
{
    private static readonly Guid LocationId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid UnknownLocationId = Guid.Parse("20000000-0000-0000-0000-000000000099");
    private static readonly Guid FirstDataSetDefinitionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondDataSetDefinitionId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    [TestMethod]
    public async Task GetAsync_UnknownLocation_ReturnsNotFound()
    {
        var service = CreateService(
            [CreateLocation(LocationId)],
            [CreateDataSetDefinition(FirstDataSetDefinitionId, "TEST", LocationId, [new DataFileFilterAndAdjustment { Id = "STATION1" }])],
            new StubStationMetadataLookup());

        var result = await service.GetAsync(UnknownLocationId);

        Assert.IsFalse(result.LocationFound);
        Assert.HasCount(0, result.SourceMetadata);
    }

    [TestMethod]
    public async Task GetAsync_KnownLocationWithNoDatasets_ReturnsEmptyList()
    {
        var service = CreateService(
            [CreateLocation(LocationId)],
            [CreateDataSetDefinition(FirstDataSetDefinitionId, "TEST", UnknownLocationId, [new DataFileFilterAndAdjustment { Id = "STATION1" }])],
            new StubStationMetadataLookup());

        var result = await service.GetAsync(LocationId);

        Assert.IsTrue(result.LocationFound);
        Assert.HasCount(0, result.SourceMetadata);
    }

    [TestMethod]
    public async Task GetAsync_KnownLocationWithMultipleDatasets_ReturnsOrderedMetadata()
    {
        var service = CreateService(
            [CreateLocation(LocationId)],
            [
                CreateDataSetDefinition(SecondDataSetDefinitionId, "ZZZ", LocationId, [new DataFileFilterAndAdjustment { Id = "STATION2" }]),
                CreateDataSetDefinition(FirstDataSetDefinitionId, "AAA", LocationId, [new DataFileFilterAndAdjustment { Id = "STATION1" }]),
            ],
            new StubStationMetadataLookup(
                new Station { Id = "STATION1", Name = "Station One" },
                new Station { Id = "STATION2", Name = "Station Two" }));

        var result = await service.GetAsync(LocationId);

        Assert.IsTrue(result.LocationFound);
        Assert.HasCount(2, result.SourceMetadata);
        Assert.AreEqual(FirstDataSetDefinitionId, result.SourceMetadata[0].DataSetDefinitionId);
        Assert.AreEqual("AAA", result.SourceMetadata[0].SourceCode);
        Assert.AreEqual("STATION1", result.SourceMetadata[0].Stations.Single().StationId);
        Assert.AreEqual(SecondDataSetDefinitionId, result.SourceMetadata[1].DataSetDefinitionId);
        Assert.AreEqual("ZZZ", result.SourceMetadata[1].SourceCode);
        Assert.AreEqual("STATION2", result.SourceMetadata[1].Stations.Single().StationId);
    }

    [TestMethod]
    public async Task GetAsync_DatasetWithMultipleStations_ReturnsAllStations()
    {
        var service = CreateService(
            [CreateLocation(LocationId)],
            [
                CreateDataSetDefinition(
                    FirstDataSetDefinitionId,
                    "TEST",
                    LocationId,
                    [
                        new DataFileFilterAndAdjustment
                        {
                            Id = "STATION1",
                            StartDate = new DateOnly(1900, 1, 1),
                            EndDate = new DateOnly(1950, 12, 31),
                        },
                        new DataFileFilterAndAdjustment
                        {
                            Id = "STATION2",
                            StartDate = new DateOnly(1951, 1, 1),
                        },
                    ]),
            ],
            new StubStationMetadataLookup(
                new Station { Id = "STATION1", Name = "Station One" },
                new Station { Id = "STATION2", Name = "Station Two", LastYear = 2024 }));

        var result = await service.GetAsync(LocationId);

        Assert.IsTrue(result.LocationFound);
        var stations = result.SourceMetadata.Single().Stations;
        Assert.HasCount(2, stations);
        Assert.AreEqual("STATION1", stations[0].StationId);
        Assert.AreEqual("Station One", stations[0].StationName);
        Assert.AreEqual(new DateOnly(1900, 1, 1), stations[0].StationStartDate);
        Assert.AreEqual(new DateOnly(1950, 12, 31), stations[0].StationEndDate);
        Assert.AreEqual("STATION2", stations[1].StationId);
        Assert.AreEqual("Station Two", stations[1].StationName);
        Assert.AreEqual(new DateOnly(1951, 1, 1), stations[1].StationStartDate);
        Assert.AreEqual(new DateOnly(2024, 12, 31), stations[1].StationEndDate);
    }

    private static LocationDataSetMetadataService CreateService(
        IEnumerable<Location> locations,
        List<DataSetDefinition> dataSetDefinitions,
        IStationMetadataLookup stationLookup)
    {
        return new LocationDataSetMetadataService(
            getLocations: () => Task.FromResult(locations),
            getDataSetDefinitions: () => Task.FromResult(dataSetDefinitions),
            sourceMetadataBuilder: new DataSetMetadataBuilder(stationLookup, id => Task.FromResult(CreateGeographicalEntity(id))));
    }

    private static Location CreateLocation(Guid locationId)
    {
        return new Location
        {
            Id = locationId,
            Name = "Testville",
            CountryCode = "AU",
            Coordinates = new Coordinates(0, 0),
        };
    }

    private static GeographicalEntity CreateGeographicalEntity(Guid locationId)
    {
        return new GeographicalEntity
        {
            Id = locationId,
            Name = locationId == LocationId ? "Testville" : "Elsewhere",
        };
    }

    private static DataSetDefinition CreateDataSetDefinition(
        Guid dataSetDefinitionId,
        string shortName,
        Guid locationId,
        List<DataFileFilterAndAdjustment> mappings)
    {
        return new DataSetDefinition
        {
            Id = dataSetDefinitionId,
            Name = $"{shortName} dataset",
            ShortName = shortName,
            MoreInformationUrl = $"https://example.test/{shortName}/dataset",
            LocationInfoUrl = $"https://example.test/{shortName}/location/[primaryStation]",
            StationInfoUrl = $"https://example.test/{shortName}/station/[station]",
            StationMetadataFileName = $"Stations_{shortName}.json",
            DataLocationMapping = new DataFileMapping
            {
                DataSetDefinitionId = dataSetDefinitionId,
                LocationIdToDataFileMappings =
                {
                    [locationId] = mappings,
                },
            },
        };
    }

    private sealed class StubStationMetadataLookup : IStationMetadataLookup
    {
        private readonly Dictionary<string, Station> stations;

        public StubStationMetadataLookup(params Station[] stations)
        {
            this.stations = stations.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        }

        public Task<Station?> GetStationAsync(DataSetDefinition dataSetDefinition, string stationId)
        {
            return Task.FromResult(stations.TryGetValue(stationId, out var station) ? station : null);
        }
    }
}
