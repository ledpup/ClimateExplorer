namespace ClimateExplorer.UnitTests;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class DirectHttpDataSetDownloaderTests
{
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerDirectDownloadTests-{Guid.NewGuid():N}");
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
    public async Task DownloadAsync_ValidResponse_WritesCandidateAtStableRelativePath()
    {
        using var client = CreateClient(HttpStatusCode.OK, "2026,7,1.25");
        var downloader = new DirectHttpDataSetDownloader(new DataSetHttpFileDownloader(client));
        var request = CreateRequest();

        var result = await downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None);

        Assert.AreEqual(Path.Combine(temporaryRoot, "TestDataset", "source.csv"), result.CandidateFilePath);
        Assert.AreEqual("2026,7,1.25", await File.ReadAllTextAsync(result.CandidateFilePath));
    }

    [TestMethod]
    public async Task DownloadAsync_EmptyResponse_ThrowsInvalidDataException()
    {
        using var client = CreateClient(HttpStatusCode.OK, string.Empty);
        var downloader = new DirectHttpDataSetDownloader(new DataSetHttpFileDownloader(client));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => downloader.DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None));
    }

    [TestMethod]
    public async Task DownloadAsync_HtmlResponse_ThrowsInvalidDataException()
    {
        using var client = CreateClient(HttpStatusCode.OK, "<!doctype html><html><body>Not data</body></html>");
        var downloader = new DirectHttpDataSetDownloader(new DataSetHttpFileDownloader(client));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => downloader.DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None));
    }

    [TestMethod]
    public async Task DownloadAsync_NonSuccessStatus_ThrowsHttpRequestException()
    {
        using var client = CreateClient(HttpStatusCode.ServiceUnavailable, "Unavailable");
        var downloader = new DirectHttpDataSetDownloader(new DataSetHttpFileDownloader(client));

        await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => downloader.DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None));
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string content)
    {
        return new HttpClient(new StubHttpMessageHandler(statusCode, content));
    }

    private static DataSetDownloadRequest CreateRequest()
    {
        var measurement = new MeasurementDefinition
        {
            DataType = DataType.TempMean,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
            DataResolution = DataResolution.Monthly,
            DataFileSource = new DataFileSourceDefinition { FilePathFormat = @"TestDataset\source.csv" },
            DataRowRegEx = @"^(?<year>\d{4}),(?<month>\d+),(?<value>-?\d+\.\d+)$",
            NullValue = "-999",
        };
        var dataSet = new DataSetDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Test dataset",
            ShortName = "Test",
            MeasurementDefinitions = [measurement],
        };

        return new DataSetDownloadRequest(
            dataSet,
            "direct-http",
            "asset",
            @"TestDataset\source.csv",
            "https://example.test/source.csv",
            [new DataSetDownloadMeasurement(measurement, new DataFileFilterAndAdjustment { Id = string.Empty })]);
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content),
            });
        }
    }
}
