namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class BomDailyDataClientTests
{
    [TestMethod]
    public async Task DownloadCsvAsync_NoCachedToken_FetchesTokenAndStoresIt()
    {
        var handler = new RecordingBomHttpMessageHandler();
        var tokenStore = new InMemoryTokenStore();
        var client = new BomDailyDataClient(new HttpClient(handler), tokenStore: tokenStore);

        var content = await client.DownloadCsvAsync("001019", BomDailyObservationCode.TemperatureMaximum, CancellationToken.None);

        Assert.IsFalse(string.IsNullOrWhiteSpace(content));
        Assert.HasCount(2, handler.Requests, "First call with no cached token should make both the availableYears and zip requests.");
        Assert.IsTrue(handler.Requests[0].Contains("availableYears", StringComparison.Ordinal));
        Assert.IsTrue(handler.Requests[1].Contains("dailyZippedDataFile", StringComparison.Ordinal));
        var storedToken = await tokenStore.GetAsync("001019", BomDailyObservationCode.TemperatureMaximum, CancellationToken.None);
        Assert.AreEqual("-2678", storedToken);
    }

    [TestMethod]
    public async Task DownloadCsvAsync_CachedTokenPresent_SkipsAvailableYearsRequest()
    {
        var handler = new RecordingBomHttpMessageHandler();
        var tokenStore = new InMemoryTokenStore();
        await tokenStore.PutAsync("001019", BomDailyObservationCode.TemperatureMaximum, "-2678", CancellationToken.None);
        var client = new BomDailyDataClient(new HttpClient(handler), tokenStore: tokenStore);

        var content = await client.DownloadCsvAsync("001019", BomDailyObservationCode.TemperatureMaximum, CancellationToken.None);

        Assert.IsFalse(string.IsNullOrWhiteSpace(content));
        Assert.HasCount(1, handler.Requests);
        Assert.IsTrue(handler.Requests[0].Contains("dailyZippedDataFile", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task DownloadCsvAsync_CachedTokenNowRejected_InvalidatesCacheAndThrowsWithoutRetrying()
    {
        var handler = new RecordingBomHttpMessageHandler(rejectZipDownload: true);
        var tokenStore = new InMemoryTokenStore();
        await tokenStore.PutAsync("001019", BomDailyObservationCode.TemperatureMaximum, "-2678", CancellationToken.None);
        var client = new BomDailyDataClient(new HttpClient(handler), tokenStore: tokenStore);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => client.DownloadCsvAsync("001019", BomDailyObservationCode.TemperatureMaximum, CancellationToken.None));

        Assert.HasCount(1, handler.Requests, "A stale cached token should not be retried inline - that would add a request on top of the one that just failed.");
        var storedToken = await tokenStore.GetAsync("001019", BomDailyObservationCode.TemperatureMaximum, CancellationToken.None);
        Assert.IsNull(storedToken, "The stale token should be invalidated so the next independent attempt fetches a fresh one.");
    }

    private sealed class InMemoryTokenStore : IBomAvailableYearsTokenStore
    {
        private readonly Dictionary<(string StationId, BomDailyObservationCode ObservationCode), string> tokens = [];

        public Task<string?> GetAsync(string stationId, BomDailyObservationCode observationCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(tokens.TryGetValue((stationId, observationCode), out var token) ? token : null);
        }

        public Task PutAsync(string stationId, BomDailyObservationCode observationCode, string token, CancellationToken cancellationToken)
        {
            tokens[(stationId, observationCode)] = token;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string stationId, BomDailyObservationCode observationCode, CancellationToken cancellationToken)
        {
            tokens.Remove((stationId, observationCode));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBomHttpMessageHandler(bool rejectZipDownload = false) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            Requests.Add(url);

            if (url.Contains("availableYears", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("001019||,1990:-2678,"),
                });
            }

            if (rejectZipDownload)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            }

            var csv = "Product code,Bureau of Meteorology station number,Year,Month,Day,Maximum temperature (Degree C),Days,Quality\nIDCJAC0010,001019,2024,01,01,30,,";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateZip(csv)),
            });
        }

        private static byte[] CreateZip(string csv)
        {
            using var output = new MemoryStream();
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry("data.csv");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(csv);
            }

            return output.ToArray();
        }
    }
}
