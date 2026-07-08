namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataServiceTests
{
    private static readonly Guid LocationId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [TestMethod]
    public async Task GetLocationDataSetMetadata_NotCached_CallsEndpointAndCachesResult()
    {
        var cache = new StubDataServiceCache();
        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(
            [
                new DataSetMetadata
                {
                    DataSetDefinitionId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    LocationId = LocationId,
                    LocationName = "Testville",
                    SourceCode = "TEST",
                    SourceName = "Test dataset",
                    Stations =
                    [
                        new DataSetStationMetadata
                        {
                            StationId = "STATION1",
                            StationName = "Station One",
                            StationStartDate = new DateOnly(2000, 1, 1),
                            StationEndDate = new DateOnly(2020, 12, 31),
                        },
                    ],
                },
            ]));
        var dataService = CreateDataService(handler, cache);

        var result = await dataService.GetLocationDataSetMetadata(LocationId);

        Assert.HasCount(1, result);
        Assert.HasCount(1, handler.Requests);
        Assert.AreEqual($"/location-dataset-metadata?locationId={LocationId}", handler.Requests[0].RequestUri!.PathAndQuery);
        Assert.AreEqual("TEST", result[0].SourceCode);
        Assert.AreEqual("STATION1", result[0].Stations[0].StationId);
        Assert.IsTrue(cache.ContainsKey($"/location-dataset-metadata?locationId={LocationId}"));
    }

    [TestMethod]
    public async Task GetLocationDataSetMetadata_Cached_ReturnsCachedResultWithoutHttpRequest()
    {
        var cache = new StubDataServiceCache();
        var cachedMetadata = new[]
        {
            new DataSetMetadata
            {
                DataSetDefinitionId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                LocationId = LocationId,
                SourceCode = "CACHED",
            },
        };
        cache.Put($"/location-dataset-metadata?locationId={LocationId}", cachedMetadata);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var dataService = CreateDataService(handler, cache);

        var result = await dataService.GetLocationDataSetMetadata(LocationId);

        Assert.HasCount(1, result);
        Assert.AreEqual("CACHED", result[0].SourceCode);
        Assert.HasCount(0, handler.Requests);
    }

    [TestMethod]
    public async Task GetLocationDataSetMetadata_NonSuccessStatusCode_ThrowsExceptionWithBody()
    {
        var cache = new StubDataServiceCache();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("missing location"),
        });
        var dataService = CreateDataService(handler, cache);

        var exception = await Assert.ThrowsExactlyAsync<Exception>(() => dataService.GetLocationDataSetMetadata(LocationId));

        StringAssert.Contains(exception.Message, "NotFound");
        StringAssert.Contains(exception.Message, "missing location");
    }

    private static DataService CreateDataService(StubHttpMessageHandler handler, IDataServiceCache cache)
    {
        return new DataService(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            cache);
    }

    private static HttpResponseMessage CreateJsonResponse(DataSetMetadata[] metadata)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web))),
        };
    }

    private sealed class StubDataServiceCache : IDataServiceCache
    {
        private readonly Dictionary<string, object> entries = [];

        public T? Get<T>(string key)
            where T : class
        {
            return entries.TryGetValue(key, out var value) ? value as T : null;
        }

        public void Put<T>(string key, T val)
            where T : class
        {
            entries[key] = val;
        }

        public bool ContainsKey(string key)
        {
            return entries.ContainsKey(key);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> createResponse;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse)
        {
            this.createResponse = createResponse;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(createResponse(request));
        }
    }
}
