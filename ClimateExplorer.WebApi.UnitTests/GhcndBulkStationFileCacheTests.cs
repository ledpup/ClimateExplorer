namespace ClimateExplorer.WebApi.UnitTests;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnd;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class GhcndBulkStationFileCacheTests
{
    private string downloadFolder = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        downloadFolder = Path.Combine(Path.GetTempPath(), $"ClimateExplorerGhcndBulkTests-{Guid.NewGuid():N}");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(downloadFolder))
        {
            Directory.Delete(downloadFolder, true);
        }
    }

    [TestMethod]
    public async Task DownloadStationsAsync_StationNotInGhcnd_SkipsItAndDownloadsTheRest()
    {
        using var httpClient = new HttpClient(new StatusByStationHandler(missingStationId: "NZM00093780", otherStatus: HttpStatusCode.OK));

        var notInGhcnd = await GhcndBulkStationFileCache.DownloadStationsAsync(
            [CreateStation("NZM00093780"), CreateStation("SPE00120512")],
            httpClient,
            downloadFolder,
            NullLogger.Instance);

        CollectionAssert.AreEqual(new[] { "NZM00093780" }, notInGhcnd.ToArray());
        Assert.IsFalse(File.Exists(GhcndBulkStationFileCache.GetCsvFilePathAndName(downloadFolder, "NZM00093780")));
        Assert.IsTrue(File.Exists(GhcndBulkStationFileCache.GetCsvFilePathAndName(downloadFolder, "SPE00120512")));
    }

    [TestMethod]
    public async Task DownloadStationsAsync_ServerError_Throws()
    {
        using var httpClient = new HttpClient(new StatusByStationHandler(missingStationId: null, otherStatus: HttpStatusCode.InternalServerError));

        await Assert.ThrowsExactlyAsync<Exception>(() => GhcndBulkStationFileCache.DownloadStationsAsync(
            [CreateStation("SPE00120512")],
            httpClient,
            downloadFolder,
            NullLogger.Instance));
    }

    private static Station CreateStation(string id) => new() { Id = id, Name = id, CountryCode = "XX" };

    // Returns 404 for the missing station's CSV, and otherStatus (with a small CSV when OK) for every other station.
    private sealed class StatusByStationHandler(string? missingStationId, HttpStatusCode otherStatus) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (missingStationId != null && request.RequestUri!.AbsolutePath.EndsWith($"/{missingStationId}.csv", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(otherStatus)
            {
                Content = new StringContent("\"STATION\",\"DATE\"\n"),
            });
        }
    }
}
