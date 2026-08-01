namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Ecad;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class EcadDataSetDownloaderTests
{
    private const string GhcnStationId = "NLM00006260";
    private const string EcadStationId = "ecad_0000162";
    private static readonly string RelativePath = Path.Combine("Ecad", "Unadjusted", $"{GhcnStationId}.zip");

    private string temporaryRoot = null!;
    private string sourceRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerEcadDownloadTests-{Guid.NewGuid():N}");
        sourceRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerEcadSourceTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        Directory.CreateDirectory(sourceRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        foreach (var directory in new[] { temporaryRoot, sourceRoot }.Where(Directory.Exists))
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task DownloadAsync_PreviouslyPublishedFile_OnlyRequestsDaysAfterItsLastRecord()
    {
        SeedPublishedArchive(["20260628,17,20,14,0", "20260629,18,21,15,1"]);
        var handler = CreateHandler(new DateOnly(2026, 6, 30), 2);

        await CreateDownloader(handler).DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None);

        Assert.HasCount(1, handler.ObservationQueries);
        StringAssert.Contains(handler.ObservationQueries.Single(), "datetime=2026-06-30T00:00:00Z/");
    }

    [TestMethod]
    public async Task DownloadAsync_NewObservations_KeepsPreviouslyPublishedRowsAlongsideThem()
    {
        SeedPublishedArchive(["20260628,17,20,14,0", "20260629,18,21,15,1"]);
        var handler = CreateHandler(new DateOnly(2026, 6, 30), 2);

        var artifact = await CreateDownloader(handler).DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None);

        var lines = EcadStationArchiveBuilder.ReadArchive(artifact.CandidateFilePath).Where(x => x.Length > 0).ToList();
        Assert.HasCount(4, lines);
        Assert.AreEqual("20260628,17,20,14,0", lines[0]);
        Assert.AreEqual("20260701,1.1,2.1,0.1,3.1", lines[3]);
    }

    [TestMethod]
    public async Task DownloadAsync_StationReportingAnUnusualAccumulationVariant_StillYieldsValues()
    {
        // The station reports the 12-12 UTC variant of every measurement rather than the common one; the
        // downloader asks for the whole family, so which variant a station uses does not matter.
        SeedPublishedArchive(["20260629,18,21,15,1"]);
        var handler = CreateHandler(
            new DateOnly(2026, 6, 30),
            2,
            parameterCodes: new Dictionary<DataType, string>
            {
                [DataType.TempMean] = "tg13",
                [DataType.TempMax] = "tx11",
                [DataType.TempMin] = "tn9",
                [DataType.Precipitation] = "rr17",
            });

        var artifact = await CreateDownloader(handler).DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None);

        var lines = EcadStationArchiveBuilder.ReadArchive(artifact.CandidateFilePath).Where(x => x.Length > 0).ToList();
        Assert.AreEqual("20260701,1.1,2.1,0.1,3.1", lines[^1]);
    }

    [TestMethod]
    public async Task DownloadAsync_ValuesFlaggedAsSuspect_AreNotPublished()
    {
        SeedPublishedArchive(["20260629,18,21,15,1"]);
        var handler = CreateHandler(new DateOnly(2026, 6, 30), 2, suspectDate: new DateOnly(2026, 7, 1));

        var artifact = await CreateDownloader(handler).DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None);

        var content = string.Join("\n", EcadStationArchiveBuilder.ReadArchive(artifact.CandidateFilePath));
        StringAssert.Contains(content, "20260630");
        Assert.DoesNotContain("20260701", content, "A value ECA&D flagged as suspect must not be published.");
    }

    [TestMethod]
    public async Task DownloadAsync_SourceHasNothingNewerThanWhatIsPublished_RepublishesWhatIsAlreadyThere()
    {
        // An up-to-date source answers with a 404, which is routine rather than a failure: it is what every
        // refresh gets between one day's observations being published and the next.
        SeedPublishedArchive(["20260628,17,20,14,0", "20260629,18,21,15,1"]);
        var handler = CreateHandler(new DateOnly(2026, 6, 30), 0);

        var artifact = await CreateDownloader(handler).DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None);

        var lines = EcadStationArchiveBuilder.ReadArchive(artifact.CandidateFilePath).Where(x => x.Length > 0).ToList();
        CollectionAssert.AreEqual(new[] { "20260628,17,20,14,0", "20260629,18,21,15,1" }, lines);
    }

    [TestMethod]
    public async Task DownloadAsync_StationMissingFromTheCrosswalk_FailsLoudly()
    {
        var downloader = new EcadDataSetDownloader(
            new EcadApiClient(new HttpClient(CreateHandler(new DateOnly(2026, 6, 30), 2)) { BaseAddress = new Uri(EcadConstants.BaseUrl) }),
            new DataSetSourceFileStore(sourceRoot),
            new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => downloader.DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None));

        StringAssert.Contains(exception.Message, GhcnStationId);
    }

    [TestMethod]
    public async Task DownloadAsync_StationTheApiDoesNotRecognise_FailsLoudly()
    {
        SeedPublishedArchive(["20260629,18,21,15,1"]);
        var handler = CreateHandler(new DateOnly(2026, 6, 30), 2, unknownStation: true);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => CreateDownloader(handler).DownloadAsync(CreateRequest(), temporaryRoot, CancellationToken.None));
    }

    [TestMethod]
    public async Task DownloadAsync_DownloadedArchive_PassesTheValidationTheRuntimeApplies()
    {
        SeedPublishedArchive(["20260628,17,20,14,0"]);
        var handler = CreateHandler(new DateOnly(2026, 6, 29), 3);
        var request = CreateRequest();

        await CreateDownloader(handler).DownloadAsync(request, temporaryRoot, CancellationToken.None);

        var latestRecordDate = await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);
        Assert.AreEqual(new DateOnly(2026, 7, 1), latestRecordDate);
    }

    private static DataSetDownloadRequest CreateRequest()
    {
        var definition = DataSetDefinitionsBuilder.BuildDataSetDefinitions()
            .Single(x => x.Id == DataSetDefinitionsBuilder.EcadDataSetDefinitionId);

        return new DataSetDownloadRequest(
            definition,
            "ecad-station",
            RelativePath.Replace('\\', '/').ToUpperInvariant(),
            RelativePath,
            null,
            [.. definition.MeasurementDefinitions!.Select(x =>
                new DataSetDownloadMeasurement(x, new DataFileFilterAndAdjustment { Id = GhcnStationId }))]);
    }

    private EcadDataSetDownloader CreateDownloader(HttpMessageHandler handler)
    {
        return new EcadDataSetDownloader(
            new EcadApiClient(new HttpClient(handler) { BaseAddress = new Uri(EcadConstants.BaseUrl) }),
            new DataSetSourceFileStore(sourceRoot),
            new Dictionary<string, string>(StringComparer.Ordinal) { [GhcnStationId] = EcadStationId });
    }

    private void SeedPublishedArchive(IEnumerable<string> lines)
    {
        var path = Path.Combine(sourceRoot, RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var archive = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create);
        var entry = archive.CreateEntry(EcadStationArchiveBuilder.GetArchiveEntryPath(GhcnStationId));
        using var writer = new StreamWriter(entry.Open());
        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }

    private static FakeEcadHandler CreateHandler(
        DateOnly firstDate,
        int dayCount,
        IReadOnlyDictionary<DataType, string>? parameterCodes = null,
        DateOnly? suspectDate = null,
        bool unknownStation = false)
    {
        return new FakeEcadHandler(
            firstDate,
            dayCount,
            parameterCodes ?? new Dictionary<DataType, string>
            {
                [DataType.TempMean] = "tg21",
                [DataType.TempMax] = "tx3",
                [DataType.TempMin] = "tn3",
                [DataType.Precipitation] = "rr7",
            },
            suspectDate,
            unknownStation);
    }

    /// <summary>
    /// Stands in for the collection, answering the parameter catalogue request and the station data
    /// queries, and reproducing the status codes the real API uses: 404 for a window with no observations,
    /// 400 for a station it does not know.
    /// </summary>
    private sealed class FakeEcadHandler(
        DateOnly firstDate,
        int dayCount,
        IReadOnlyDictionary<DataType, string> parameterCodes,
        DateOnly? suspectDate,
        bool unknownStation) : HttpMessageHandler
    {
        public List<string> ObservationQueries { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!.ToString();
            if (!uri.Contains("/locations/", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(HttpStatusCode.OK, BuildCatalogue()));
            }

            ObservationQueries.Add(uri);
            if (unknownStation)
            {
                return Task.FromResult(Json(
                    HttpStatusCode.BadRequest,
                    $"{{\"detail\":\"Out of the queried stations '{EcadStationId}' the following stations '{EcadStationId}' do not exist.\"}}"));
            }

            return Task.FromResult(dayCount == 0
                ? Json(HttpStatusCode.NotFound, "{\"detail\":\"The query returned no data for the selected stations.\"}")
                : Json(HttpStatusCode.OK, BuildCoverage()));
        }

        private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        {
            return new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }

        /// <summary>The real catalogue's variants are not contiguous, so neither are these.</summary>
        private static string BuildCatalogue()
        {
            var names = new[] { "tg5", "tg13", "tg21", "tx3", "tx11", "tn3", "tn9", "rr7", "rr17", "cc1", "sd3" };
            return "{\"parameter_names\":{" + string.Join(',', names.Select(x => $"\"{x}\":{{}}")) + "}}";
        }

        private string BuildCoverage()
        {
            var dates = Enumerable.Range(0, dayCount).Select(firstDate.AddDays).ToList();
            var times = string.Join(',', dates.Select(x => $"\"{x:yyyy-MM-dd}T00:00:00Z\""));

            // Distinct per measurement and per day, so a column landing in the wrong place is visible in
            // the assertion rather than hidden behind identical numbers.
            var baseValues = new Dictionary<DataType, double>
            {
                [DataType.TempMean] = 1d,
                [DataType.TempMax] = 2d,
                [DataType.TempMin] = 0d,
                [DataType.Precipitation] = 3d,
            };

            var ranges = new List<string>();
            foreach (var dataType in EcadConstants.PublishedDataTypes)
            {
                var code = parameterCodes[dataType];
                var values = string.Join(',', dates.Select((_, i) =>
                    (baseValues[dataType] + (i * 0.1)).ToString("0.0#", CultureInfo.InvariantCulture)));
                var flags = string.Join(',', dates.Select(x => x == suspectDate ? "1" : "0"));
                ranges.Add($"\"{code}\":{{\"type\":\"NdArray\",\"dataType\":\"float\",\"values\":[{values}]}}");
                ranges.Add($"\"{code}_q\":{{\"type\":\"NdArray\",\"dataType\":\"integer\",\"values\":[{flags}]}}");
            }

            return "{\"type\":\"CoverageCollection\",\"coverages\":[{\"type\":\"Coverage\",\"domain\":{\"axes\":{\"t\":{\"values\":[" +
                times + "]}}},\"ranges\":{" + string.Join(',', ranges) + "}}]}";
        }
    }
}
