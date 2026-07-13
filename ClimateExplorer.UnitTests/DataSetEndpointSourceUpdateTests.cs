namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class DataSetEndpointSourceUpdateTests
{
    [TestMethod]
    public async Task PostDataSets_FreshCachedResponse_ReturnsExistingCachedData()
    {
        var body = CreateMinimalRequest();
        var cachedData = new DataSet { RetrievedDate = DateTimeOffset.UtcNow };
        var cache = new MemoryCache();
        await cache.Put(GetCacheKey(body), cachedData);
        var coordinator = new StubSourceUpdateCoordinator(
            new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.UseCached));
        var services = CreateServices(cache, coordinator);

        var result = await DataSetEndpoints.PostDataSets(body, services);

        Assert.AreSame(cachedData, result);
        Assert.AreEqual(1, coordinator.CallCount);
    }

    [TestMethod]
    public async Task PostDataSets_RefreshFailsWithCachedResponse_ReturnsExistingCachedDataUnchanged()
    {
        var body = CreateMinimalRequest();
        var retrievedDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var cachedData = new DataSet { RetrievedDate = retrievedDate };
        var cache = new MemoryCache();
        await cache.Put(GetCacheKey(body), cachedData);
        var coordinator = new StubSourceUpdateCoordinator(
            new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed));
        var services = CreateServices(cache, coordinator);

        var result = await DataSetEndpoints.PostDataSets(body, services);

        Assert.AreSame(cachedData, result);
        Assert.AreEqual(retrievedDate, result.RetrievedDate);
    }

    [TestMethod]
    public async Task PostDataSets_RefreshFailsWithoutCachedResponse_BuildsFromPackagedSourceWithNullRetrievedDate()
    {
        var body = await CreateNinoRequest();
        var cache = new MemoryCache();
        var coordinator = new StubSourceUpdateCoordinator(
            new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed));
        var services = CreateServices(cache, coordinator);

        var result = await DataSetEndpoints.PostDataSets(body, services);

        Assert.IsGreaterThan(0, result.DataRecords.Count);
        Assert.IsNull(result.RetrievedDate);
    }

    [TestMethod]
    public async Task PostDataSets_RefreshSucceeds_RebuildsAndUsesSuccessfulRetrievalDate()
    {
        var body = await CreateNinoRequest();
        var retrievedDate = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var cache = new MemoryCache();
        var coordinator = new StubSourceUpdateCoordinator(
            new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.Rebuild, retrievedDate));
        var services = CreateServices(cache, coordinator);

        var result = await DataSetEndpoints.PostDataSets(body, services);

        Assert.IsGreaterThan(0, result.DataRecords.Count);
        Assert.AreEqual(retrievedDate, result.RetrievedDate);
    }

    private static PostDataSetsRequestBody CreateMinimalRequest()
    {
        return new PostDataSetsRequestBody
        {
            BinningRule = BinGranularities.ByYearAndMonth,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = 1,
        };
    }

    private static async Task<PostDataSetsRequestBody> CreateNinoRequest()
    {
        var definition = (await DataSetDefinition.GetDataSetDefinitions()).Single(x => x.ShortName == "Niño 3.4");
        var locationId = definition.DataLocationMapping!.LocationIdToDataFileMappings.Keys.First();

        return new PostDataSetsRequestBody
        {
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = definition.Id,
                    DataType = DataType.Nino34,
                    LocationId = locationId,
                },
            ],
            BinningRule = BinGranularities.ByYearAndMonth,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            BucketAggregationFunction = ContainerAggregationFunctions.Mean,
            CupAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = 1,
        };
    }

    private static ClimateExplorerApiServices CreateServices(
        ICache cache,
        IDataSetSourceUpdateCoordinator coordinator)
    {
        return new ClimateExplorerApiServices(cache, new MemoryCache(), new HttpClient(), new HttpClient(), coordinator);
    }

    private static string GetCacheKey(PostDataSetsRequestBody body)
    {
        return "DataSet_v2_" + JsonSerializer.Serialize(body);
    }

    private sealed class StubSourceUpdateCoordinator(DataSetSourcePreparationResult result) : IDataSetSourceUpdateCoordinator
    {
        public int CallCount { get; private set; }

        public Task<DataSetSourcePreparationResult> PrepareAsync(
            PostDataSetsRequestBody request,
            DataSet? cachedData,
            bool permitSourceUpdate,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class MemoryCache : ICache
    {
        private readonly Dictionary<string, object> values = [];

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
