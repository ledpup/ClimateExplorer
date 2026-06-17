namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
[DoNotParallelize]
public sealed class RecentObservationsEndpointTests
{
    private static readonly Guid GhcndLocationId = Guid.Parse("67f08df4-76e9-4087-8687-25ee09cbedc5");

    [TestMethod]
    public async Task GetRecentObservationsReturnsGhcndTemperatureRecords()
    {
        var cache = CreateCacheWithLocation();
        var ghcndHandler = new StubHttpMessageHandler(CreateGhcndCsv());
        var services = CreateServices(cache, ghcndHandler);

        var response = await RunFromWebApiDirectory(
            () => RecentObservationsEndpoints.GetRecentObservations(
                services,
                LoggerFactory.Create(_ => { }),
                GhcndLocationId,
                DataType.TempMax));

        Assert.IsTrue(response.IsSupported);
        Assert.AreEqual(DataType.TempMax, response.DataType);
        Assert.AreEqual(DataAdjustment.Unadjusted, response.DataAdjustment);
        Assert.AreEqual(DataResolution.Daily, response.DataResolution);
        Assert.AreEqual(UnitOfMeasure.DegreesCelsius, response.UnitOfMeasure);
        Assert.HasCount(2, response.Records);
        Assert.AreEqual(new DateOnly(DateTime.Today.Year - 1, 1, 1), response.Records[0].Date);
        Assert.AreEqual(30.5d, response.Records[0].Value);
        Assert.AreEqual(new DateOnly(DateTime.Today.Year, 6, 16), response.Records[1].Date);
        Assert.AreEqual(41.1d, response.Records[1].Value);
        StringAssert.Contains(ghcndHandler.Requests[0].RequestUri!.ToString(), "AE000041196.csv");
    }

    [TestMethod]
    public async Task GetRecentObservationsReturnsGhcndPrecipitationRecords()
    {
        var cache = CreateCacheWithLocation();
        var ghcndHandler = new StubHttpMessageHandler(CreateGhcndCsv());
        var services = CreateServices(cache, ghcndHandler);

        var response = await RunFromWebApiDirectory(
            () => RecentObservationsEndpoints.GetRecentObservations(
                services,
                LoggerFactory.Create(_ => { }),
                GhcndLocationId,
                DataType.Precipitation));

        Assert.IsTrue(response.IsSupported);
        Assert.AreEqual(DataType.Precipitation, response.DataType);
        Assert.IsNull(response.DataAdjustment);
        Assert.AreEqual(DataResolution.Daily, response.DataResolution);
        Assert.AreEqual(UnitOfMeasure.Millimetres, response.UnitOfMeasure);
        Assert.HasCount(2, response.Records);
        Assert.AreEqual(new DateOnly(DateTime.Today.Year - 1, 1, 1), response.Records[0].Date);
        Assert.AreEqual(1.2d, response.Records[0].Value);
        Assert.AreEqual(new DateOnly(DateTime.Today.Year, 6, 16), response.Records[1].Date);
        Assert.AreEqual(0d, response.Records[1].Value);
        StringAssert.Contains(ghcndHandler.Requests[0].RequestUri!.ToString(), "AE000041196.csv");
    }

    private static MemoryCache CreateCacheWithLocation()
    {
        var cache = new MemoryCache();
        cache.Items[$"Locations_{GhcndLocationId}"] = new[]
        {
            new Location
            {
                Id = GhcndLocationId,
                Name = "GHCNd Test Location",
                CountryCode = "AE",
                Coordinates = new Coordinates(0, 0),
            },
        };

        return cache;
    }

    private static ClimateExplorerApiServices CreateServices(MemoryCache cache, StubHttpMessageHandler ghcndHandler)
    {
        return new ClimateExplorerApiServices(
            cache,
            new MemoryCache(),
            new HttpClient(new StubHttpMessageHandler(string.Empty)),
            new HttpClient(ghcndHandler));
    }

    private static string CreateGhcndCsv()
    {
        var previousYear = DateTime.Today.Year - 1;
        var currentYear = DateTime.Today.Year;

        return string.Join(
            Environment.NewLine,
            [
                "STATION,DATE,PRCP,PRCP_ATTRIBUTES,TMAX,TMAX_ATTRIBUTES,TMIN,TMIN_ATTRIBUTES",
                $"AE000041196,{previousYear}-01-01,12,\",,S\",305,\",,S\",202,\",,S\"",
                $"AE000041196,{currentYear}-06-16,0,\",,S\",411,\",,S\",299,\",,S\"",
                $"AE000041196,{currentYear}-06-17,8,\",D,S\",421,\",D,S\",301,\",,S\"",
            ]);
    }

    private static async Task<T> RunFromWebApiDirectory<T>(Func<Task<T>> action)
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(GetWebApiDirectory());

        try
        {
            return await action();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    private static string GetWebApiDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ClimateExplorer.sln")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            throw new DirectoryNotFoundException("Unable to find the ClimateExplorer solution directory.");
        }

        return Path.Combine(directory.FullName, "ClimateExplorer.WebApi");
    }

    private sealed class MemoryCache : ICache
    {
        public Dictionary<string, object> Items { get; } = [];

        public Task<T> Get<T>(string key)
        {
            return Task.FromResult(Items.TryGetValue(key, out var value) ? (T)value : default!);
        }

        public Task Put<T>(string key, T obj)
        {
            Items[key] = obj!;
            return Task.CompletedTask;
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string content;

        public StubHttpMessageHandler(string content)
        {
            this.content = content;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content),
                });
        }
    }
}
