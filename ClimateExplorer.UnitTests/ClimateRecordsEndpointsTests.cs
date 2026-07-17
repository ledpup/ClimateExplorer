namespace ClimateExplorer.UnitTests;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.WebApi;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class ClimateRecordsEndpointsTests
{
    // Location cbb11150-ec74-4401-8357-fae6fef70768 was served by BOM station 001021 until 1998-09-15,
    // then by station 001019 (still open, EndDate: null) from 1998-09-16 onward (see DataSetDownloadMetadataTests).
    private static readonly Guid MultiStationBomLocationId = Guid.Parse("cbb11150-ec74-4401-8357-fae6fef70768");

    [TestMethod]
    public async Task GetClimateRecords_KnownLocation_ReturnsSourceMetadataFromDataSet()
    {
        var ghcndLocationId = await GetSingleStationGhcndLocationId("AE000041196");

        var response = await ClimateRecordsEndpoints.GetClimateRecords(CreateServices(), ghcndLocationId, DataType.TempMax, DataAdjustment.Unadjusted);

        Assert.IsTrue(response.DataResolution.HasValue);
        Assert.IsNotNull(response.SourceMetadata);
        Assert.HasCount(1, response.SourceMetadata);
        Assert.AreEqual("AE000041196", response.SourceMetadata.Single().Stations.Single().StationId);
    }

    [TestMethod]
    public async Task GetClimateRecords_MultiStationBomLocation_SourceMetadataStationsContainsExactlyOneOpenStation()
    {
        var response = await ClimateRecordsEndpoints.GetClimateRecords(CreateServices(), MultiStationBomLocationId, DataType.TempMax, DataAdjustment.Unadjusted);

        Assert.IsNotNull(response.SourceMetadata);
        var stations = response.SourceMetadata.Single().Stations;
        Assert.IsGreaterThanOrEqualTo(2, stations.Count, "Expected the full historical station list, not just the open station.");
        var openStation = stations.SingleOrDefault(x => x.StationEndDate is null);
        Assert.IsNotNull(openStation);
        Assert.AreEqual("001019", openStation.StationId);
    }

    [TestMethod]
    public async Task GetClimateRecords_UnmatchedDataTypeForLocation_ReturnsNullDataResolutionAndNoSourceMetadata()
    {
        // SolarRadiation is not mapped for GHCNd locations, so this location/data-type pair has no matching dsd.
        var ghcndLocationId = await GetSingleStationGhcndLocationId("AE000041196");

        var response = await ClimateRecordsEndpoints.GetClimateRecords(CreateServices(), ghcndLocationId, DataType.SolarRadiation);

        Assert.IsFalse(response.DataResolution.HasValue);
        Assert.IsNull(response.SourceMetadata);
        Assert.IsEmpty(response.Records);
    }

    [TestMethod]
    public async Task GetClimateRecords_CallerCancelsRequest_PropagatesOperationCanceledException()
    {
        var ghcndLocationId = await GetSingleStationGhcndLocationId("AE000041196");
        var coordinator = new CancellingSourceUpdateCoordinator();
        var services = new ClimateExplorerApiServices(new MemoryCache(), new MemoryCache(), new HttpClient(), new HttpClient(), coordinator);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            ClimateRecordsEndpoints.GetClimateRecords(services, ghcndLocationId, DataType.TempMax, DataAdjustment.Unadjusted, cancellationToken: cts.Token));
    }

    private static async Task<Guid> GetSingleStationGhcndLocationId(string stationId)
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions();
        var definition = definitions.Single(x => x.ShortName == "GHCNd");
        return definition.DataLocationMapping!.LocationIdToDataFileMappings
            .First(x => x.Value.Any(y => y.Id == stationId)).Key;
    }

    private static ClimateExplorerApiServices CreateServices()
    {
        var coordinator = new StubSourceUpdateCoordinator(
            new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed));
        return new ClimateExplorerApiServices(new MemoryCache(), new MemoryCache(), new HttpClient(), new HttpClient(), coordinator);
    }

    private sealed class StubSourceUpdateCoordinator(DataSetSourcePreparationResult result) : IDataSetSourceUpdateCoordinator
    {
        public Task<DataSetSourcePreparationResult> PrepareAsync(
            PostDataSetsRequestBody request,
            ICachedData? cachedData,
            bool permitSourceUpdate,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class CancellingSourceUpdateCoordinator : IDataSetSourceUpdateCoordinator
    {
        public Task<DataSetSourcePreparationResult> PrepareAsync(
            PostDataSetsRequestBody request,
            ICachedData? cachedData,
            bool permitSourceUpdate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Expected cancellation to be observed before this point.");
        }
    }

    private sealed class MemoryCache : ICache
    {
        private readonly System.Collections.Generic.Dictionary<string, object> values = [];

        public Task<T> Get<T>(string key)
        {
            return Task.FromResult(values.TryGetValue(key, out var value) ? (T)value : default!);
        }

        public Task Put<T>(string key, T obj)
        {
            values[key] = obj!;
            return Task.CompletedTask;
        }
    }
}
