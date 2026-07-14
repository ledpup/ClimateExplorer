namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class NoaaGlobalTempDataSetDownloaderTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);

    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerNoaaGlobalTempDownloadTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(temporaryRoot))
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [TestMethod]
    public async Task DownloadAsync_CurrentMonthReleaseAvailable_DownloadsCurrentMonth()
    {
        var request = await ResolveRequest();
        var handler = new SequencedResponseHandler(
            new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["202607"] = OkResponse(CreateAscContent()),
            });
        var downloader = CreateDownloader(handler);

        var artifact = await downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None);

        Assert.HasCount(1, handler.RequestedReleases);
        Assert.AreEqual("202607", handler.RequestedReleases[0]);
        Assert.IsTrue(File.Exists(artifact.CandidateFilePath));
        await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);
    }

    [TestMethod]
    public async Task DownloadAsync_CurrentMonthMissing_FallsBackToOlderRelease()
    {
        var request = await ResolveRequest();
        var handler = new SequencedResponseHandler(
            new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["202607"] = new HttpResponseMessage(HttpStatusCode.NotFound),
                ["202606"] = new HttpResponseMessage(HttpStatusCode.NotFound),
                ["202605"] = OkResponse(CreateAscContent()),
            });
        var downloader = CreateDownloader(handler);

        var artifact = await downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "202607", "202606", "202605" }, handler.RequestedReleases);
        Assert.IsTrue(File.Exists(artifact.CandidateFilePath));
    }

    [TestMethod]
    public async Task DownloadAsync_NoReleaseWithinBoundedWindow_ThrowsInvalidDataException()
    {
        var request = await ResolveRequest();
        var handler = new SequencedResponseHandler(new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal));
        var downloader = CreateDownloader(handler);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None));

        Assert.HasCount(6, handler.RequestedReleases);
        Assert.IsFalse(File.Exists(Path.Combine(temporaryRoot, request.RelativePath)));
    }

    [TestMethod]
    public async Task DownloadAsync_HtmlErrorPageForNewestRelease_FallsBackToOlderRelease()
    {
        var request = await ResolveRequest();
        var handler = new SequencedResponseHandler(
            new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["202607"] = OkResponse("<!DOCTYPE html><html><body>Not found</body></html>"),
                ["202606"] = OkResponse(CreateAscContent()),
            });
        var downloader = CreateDownloader(handler);

        var artifact = await downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "202607", "202606" }, handler.RequestedReleases);
        var content = await File.ReadAllTextAsync(artifact.CandidateFilePath);
        StringAssert.Contains(content, "2024");
    }

    private static NoaaGlobalTempDataSetDownloader CreateDownloader(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new NoaaGlobalTempDataSetDownloader(
            new DataSetHttpFileDownloader(httpClient),
            new FakeTimeProvider(UtcNow));
    }

    private static HttpResponseMessage OkResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
    }

    private static string CreateAscContent()
    {
        return "2024  1  0.50\n2024  2  0.55\n2024  3  0.60\n";
    }

    private static async Task<DataSetDownloadRequest> ResolveRequest()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
        var definition = definitions.Single(x => x.ShortName == "Global temp");
        var locationId = definition.DataLocationMapping!.LocationIdToDataFileMappings
            .Single(x => x.Value.Single().Id == "land_ocean.90S.90N").Key;
        var body = new PostDataSetsRequestBody
        {
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = definition.Id,
                    DataType = DataType.TempMean,
                    DataAdjustment = null,
                    LocationId = locationId,
                },
            ],
            BinningRule = BinGranularities.ByYearAndDay,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = 1,
        };

        return (await new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"))
            .ResolveAsync(body, CancellationToken.None)).Single();
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset now = now;

        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class SequencedResponseHandler(IReadOnlyDictionary<string, HttpResponseMessage> responsesByRelease) : HttpMessageHandler
    {
        public List<string> RequestedReleases { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            var release = url[(url.LastIndexOf(".v6.0.0.", StringComparison.Ordinal) + ".v6.0.0.".Length)..].Replace(".asc", string.Empty, StringComparison.Ordinal);
            RequestedReleases.Add(release);
            return Task.FromResult(
                responsesByRelease.TryGetValue(release, out var response)
                    ? response
                    : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
