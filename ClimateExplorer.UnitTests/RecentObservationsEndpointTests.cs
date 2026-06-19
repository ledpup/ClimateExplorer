namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    private static readonly Guid BomLocationId = Guid.Parse("1e743b5c-f9bf-477c-8b16-7d45c67909a7");
    private static readonly Guid GhcndLocationId = Guid.Parse("67f08df4-76e9-4087-8687-25ee09cbedc5");

    [TestMethod]
    public async Task GetRecentObservationsReturnsBomMetadata()
    {
        var cache = CreateCacheWithLocation(CreateBomLocation());
        var bomHandler = new StubHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            return url.Contains("availableYears", StringComparison.OrdinalIgnoreCase)
                ? CreateTextResponse("086338||,1900:12345,")
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(CreateBomZip()),
                };
        });
        var services = CreateServices(cache, bomHandler, new StubHttpMessageHandler(string.Empty));

        var response = await RunFromWebApiDirectory(
            () => RecentObservationsEndpoints.GetRecentObservations(
                services,
                LoggerFactory.Create(_ => { }),
                BomLocationId,
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
        AssertSourceMetadata(
            response,
            "BOM",
            "Australian Bureau of Meteorology",
            "086338",
            bomHandler.Requests[1].RequestUri!.ToString(),
            "TempMax station 086338, obs code 122, ZIP");
    }

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
        AssertSourceMetadata(
            response,
            "GHCNd",
            "Global Historical Climatology Network Daily",
            "AE000041196",
            "https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/AE000041196.csv",
            "Station AE000041196, CSV");
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
        AssertSourceMetadata(
            response,
            "GHCNd",
            "Global Historical Climatology Network Daily",
            "AE000041196",
            "https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/AE000041196.csv",
            "Station AE000041196, CSV");
    }

    private static void AssertSourceMetadata(
        RecentObservationsResponse response,
        string expectedSourceCode,
        string expectedSourceName,
        string expectedStationId,
        string expectedSourceUrl,
        string expectedSourceUrlLabel)
    {
        Assert.IsNotNull(response.SourceMetadata);
        Assert.AreEqual(expectedSourceCode, response.SourceMetadata.SourceCode);
        Assert.AreEqual(expectedSourceName, response.SourceMetadata.SourceName);
        Assert.AreEqual(expectedStationId, response.SourceMetadata.StationId);
        Assert.AreEqual(expectedSourceUrl, response.SourceMetadata.SourceUrl);
        Assert.AreEqual(expectedSourceUrlLabel, response.SourceMetadata.SourceUrlLabel);
        Assert.IsNotNull(response.SourceMetadata.RetrievedAtUtc);
        Assert.AreEqual(TimeSpan.Zero, response.SourceMetadata.RetrievedAtUtc.Value.Offset);
        Assert.AreEqual(response.RetrievedDate, response.SourceMetadata.RetrievedAtUtc);
    }

    private static MemoryCache CreateCacheWithLocation(params Location[] locations)
    {
        var cache = new MemoryCache();

        if (locations.Length == 0)
        {
            locations = [CreateGhcndLocation()];
        }

        foreach (var location in locations)
        {
            cache.Items[$"Locations_{location.Id}"] = new[] { location };
        }

        return cache;
    }

    private static Location CreateBomLocation()
    {
        return new Location
        {
            Id = BomLocationId,
            Name = "BOM Test Location",
            CountryCode = "AU",
            Coordinates = new Coordinates(-37.66d, 144.83d),
        };
    }

    private static Location CreateGhcndLocation()
    {
        return new Location
        {
            Id = GhcndLocationId,
            Name = "GHCNd Test Location",
            CountryCode = "AE",
            Coordinates = new Coordinates(0, 0),
        };
    }

    private static ClimateExplorerApiServices CreateServices(MemoryCache cache, StubHttpMessageHandler ghcndHandler)
    {
        return CreateServices(cache, new StubHttpMessageHandler(string.Empty), ghcndHandler);
    }

    private static ClimateExplorerApiServices CreateServices(
        MemoryCache cache,
        StubHttpMessageHandler bomHandler,
        StubHttpMessageHandler ghcndHandler)
    {
        return new ClimateExplorerApiServices(
            cache,
            new MemoryCache(),
            new HttpClient(bomHandler),
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

    private static byte[] CreateBomZip()
    {
        var previousYear = DateTime.Today.Year - 1;
        var currentYear = DateTime.Today.Year;

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("bom.csv");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(
                string.Join(
                    Environment.NewLine,
                    [
                        "Product code,Station Number,Year,Month,Day,Maximum temperature (Degree C),Days of accumulation,Quality",
                        $"IDCJAC0010,086338,{previousYear},01,01,30.5,,",
                        $"IDCJAC0010,086338,{currentYear},06,16,41.1,,",
                    ]));
        }

        return stream.ToArray();
    }

    private static HttpResponseMessage CreateTextResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content),
        };
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
        private readonly Func<HttpRequestMessage, HttpResponseMessage> createResponse;

        public StubHttpMessageHandler(string content)
            : this(_ => CreateTextResponse(content))
        {
        }

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
