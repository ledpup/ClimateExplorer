namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class DataSetMetadataBuilderTests
{
    private static readonly Guid DataSetDefinitionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondDataSetDefinitionId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid LocationId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondLocationId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    [TestMethod]
    public async Task BuildAsync_SingleMappedStation_ReturnsSourceAndStationMetadata()
    {
        var dataSetDefinition = CreateDataSetDefinition(
            DataSetDefinitionId,
            LocationId,
            [
                new DataFileFilterAndAdjustment
                {
                    Id = "STATION1",
                    StartDate = new DateOnly(2000, 1, 1),
                    EndDate = new DateOnly(2020, 12, 31),
                },
            ]);
        var stationLookup = new StubStationMetadataLookup(
            new Station
            {
                Id = "STATION1",
                Name = "Station One",
                FirstYear = 1999,
                LastYear = 2021,
            });

        var result = await CreateBuilder(stationLookup).BuildAsync(CreateRequest(DataSetDefinitionId, LocationId), [dataSetDefinition]);

        Assert.HasCount(1, result);
        var source = result.Single();
        Assert.AreEqual(DataSetDefinitionId, source.DataSetDefinitionId);
        Assert.AreEqual(LocationId, source.LocationId);
        Assert.AreEqual("Testville", source.LocationName);
        Assert.AreEqual("TEST", source.SourceCode);
        Assert.AreEqual("Test dataset", source.SourceName);
        Assert.AreEqual("https://example.test/location/STATION1", source.SourceUrl);
        Assert.AreEqual("TEST", source.SourceUrlLabel);

        Assert.HasCount(1, source.Stations);
        var station = source.Stations.Single();
        Assert.AreEqual("STATION1", station.StationId);
        Assert.AreEqual("Station One", station.StationName);
        Assert.AreEqual(new DateOnly(2000, 1, 1), station.StationStartDate);
        Assert.AreEqual(new DateOnly(2020, 12, 31), station.StationEndDate);
        Assert.AreEqual("https://example.test/station/STATION1", station.SourceUrl);
        Assert.AreEqual("Station STATION1", station.SourceUrlLabel);
    }

    [TestMethod]
    public async Task BuildAsync_MultipleMappedStations_ReturnsAllStationsWithMappingDates()
    {
        var dataSetDefinition = CreateDataSetDefinition(
            DataSetDefinitionId,
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
                    EndDate = null,
                },
            ]);
        var stationLookup = new StubStationMetadataLookup(
            new Station { Id = "STATION1", Name = "Station One" },
            new Station { Id = "STATION2", Name = "Station Two", LastYear = 2024 });

        var result = await CreateBuilder(stationLookup).BuildAsync(CreateRequest(DataSetDefinitionId, LocationId), [dataSetDefinition]);

        var stations = result.Single().Stations;
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

    [TestMethod]
    public async Task BuildAsync_MissingStationDetails_ReturnsStationIdsWithoutNames()
    {
        var dataSetDefinition = CreateDataSetDefinition(
            DataSetDefinitionId,
            LocationId,
            [new DataFileFilterAndAdjustment { Id = "UNKNOWN" }]);
        var stationLookup = new StubStationMetadataLookup();

        var result = await CreateBuilder(stationLookup).BuildAsync(CreateRequest(DataSetDefinitionId, LocationId), [dataSetDefinition]);

        var station = result.Single().Stations.Single();
        Assert.AreEqual("UNKNOWN", station.StationId);
        Assert.IsNull(station.StationName);
        Assert.IsNull(station.StationStartDate);
        Assert.IsNull(station.StationEndDate);
        Assert.AreEqual("https://example.test/station/UNKNOWN", station.SourceUrl);
    }

    [TestMethod]
    public async Task BuildAsync_DerivedSeries_ReturnsMetadataForEachSourceSpecification()
    {
        var firstDefinition = CreateDataSetDefinition(
            DataSetDefinitionId,
            LocationId,
            [new DataFileFilterAndAdjustment { Id = "STATION1" }]);
        var secondDefinition = CreateDataSetDefinition(
            SecondDataSetDefinitionId,
            SecondLocationId,
            [new DataFileFilterAndAdjustment { Id = "STATION2" }],
            shortName: "TEST2",
            name: "Second dataset");
        var stationLookup = new StubStationMetadataLookup(
            new Station { Id = "STATION1", Name = "Station One" },
            new Station { Id = "STATION2", Name = "Station Two" });
        var request = new PostDataSetsRequestBody
        {
            SeriesDerivationType = SeriesDerivationTypes.DifferenceBetweenTwoSeries,
            SeriesSpecifications =
            [
                CreateSpecification(DataSetDefinitionId, LocationId),
                CreateSpecification(SecondDataSetDefinitionId, SecondLocationId),
            ],
            BinningRule = BinGranularities.ByYear,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            BucketAggregationFunction = ContainerAggregationFunctions.Mean,
            CupAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = 1,
        };

        var result = await CreateBuilder(stationLookup).BuildAsync(request, [firstDefinition, secondDefinition]);

        Assert.HasCount(2, result);
        Assert.AreEqual(DataSetDefinitionId, result[0].DataSetDefinitionId);
        Assert.AreEqual("Testville", result[0].LocationName);
        Assert.AreEqual("STATION1", result[0].Stations.Single().StationId);
        Assert.AreEqual(SecondDataSetDefinitionId, result[1].DataSetDefinitionId);
        Assert.AreEqual("Elsewhere", result[1].LocationName);
        Assert.AreEqual("TEST2", result[1].SourceCode);
        Assert.AreEqual("STATION2", result[1].Stations.Single().StationId);
    }

    [TestMethod]
    public async Task BuildAsync_DataSetDefinitionAndLocation_ReturnsSourceAndStationMetadata()
    {
        var dataSetDefinition = CreateDataSetDefinition(
            DataSetDefinitionId,
            LocationId,
            [new DataFileFilterAndAdjustment { Id = "STATION1" }]);
        var stationLookup = new StubStationMetadataLookup(
            new Station { Id = "STATION1", Name = "Station One" });

        var result = await CreateBuilder(stationLookup).BuildAsync(dataSetDefinition, LocationId);

        Assert.AreEqual(DataSetDefinitionId, result.DataSetDefinitionId);
        Assert.AreEqual(LocationId, result.LocationId);
        Assert.AreEqual("Testville", result.LocationName);
        Assert.AreEqual("TEST", result.SourceCode);
        Assert.AreEqual("Test dataset", result.SourceName);
        Assert.AreEqual("STATION1", result.Stations.Single().StationId);
        Assert.AreEqual("Station One", result.Stations.Single().StationName);
    }

    private static DataSetMetadataBuilder CreateBuilder(IStationMetadataLookup stationLookup)
    {
        return new DataSetMetadataBuilder(stationLookup, id => Task.FromResult(CreateGeographicalEntity(id)));
    }

    private static GeographicalEntity CreateGeographicalEntity(Guid id)
    {
        return new GeographicalEntity
        {
            Id = id,
            Name = id == SecondLocationId ? "Elsewhere" : "Testville",
        };
    }

    private static PostDataSetsRequestBody CreateRequest(Guid dataSetDefinitionId, Guid locationId)
    {
        return new PostDataSetsRequestBody
        {
            SeriesSpecifications = [CreateSpecification(dataSetDefinitionId, locationId)],
            BinningRule = BinGranularities.ByYear,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            BucketAggregationFunction = ContainerAggregationFunctions.Mean,
            CupAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = 1,
        };
    }

    private static SeriesSpecification CreateSpecification(Guid dataSetDefinitionId, Guid locationId)
    {
        return new SeriesSpecification
        {
            DataSetDefinitionId = dataSetDefinitionId,
            LocationId = locationId,
            DataType = DataType.TempMean,
            DataAdjustment = DataAdjustment.Adjusted,
        };
    }

    private static DataSetDefinition CreateDataSetDefinition(
        Guid dataSetDefinitionId,
        Guid locationId,
        List<DataFileFilterAndAdjustment> mappings,
        string shortName = "TEST",
        string name = "Test dataset")
    {
        return new DataSetDefinition
        {
            Id = dataSetDefinitionId,
            Name = name,
            ShortName = shortName,
            MoreInformationUrl = "https://example.test/dataset",
            LocationInfoUrl = "https://example.test/location/[primaryStation]",
            StationInfoUrl = "https://example.test/station/[station]",
            StationMetadataFileName = "Stations_Test.json",
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

