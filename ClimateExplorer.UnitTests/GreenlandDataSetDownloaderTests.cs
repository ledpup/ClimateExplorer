namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class GreenlandDataSetDownloaderTests
{
    private string temporaryRoot = null!;
    private string sourceRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerGreenlandDownloadTests-{Guid.NewGuid():N}");
        sourceRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerGreenlandSourceTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        Directory.CreateDirectory(sourceRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(temporaryRoot))
        {
            Directory.Delete(temporaryRoot, true);
        }

        if (Directory.Exists(sourceRoot))
        {
            Directory.Delete(sourceRoot, true);
        }
    }

    [TestMethod]
    public async Task DownloadAsync_NoPreviousPublishedFile_DownloadsEveryYearFromFirstYear()
    {
        // 1981-07-14 is day 195 of the year, so the current year needs at least 185 populated days.
        var now = new DateTimeOffset(1981, 7, 14, 0, 0, 0, TimeSpan.Zero);
        var handler = new FakeGreenlandHandler(
            new Dictionary<int, string>
            {
                [1979] = BuildYearJson(FullYear(1979)),
                [1980] = BuildYearJson(FullYear(1980)),
                [1981] = BuildYearJson(ConsecutiveDays(1981, 190)),
            });
        var request = await ResolveRequest();

        var artifact = await CreateDownloader(handler, now).DownloadAsync(request, temporaryRoot, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { 1979, 1980, 1981 }, handler.RequestedYears);
        var lines = await File.ReadAllLinesAsync(artifact.CandidateFilePath);
        Assert.HasCount(365 + 366 + 190, lines);
        Assert.AreEqual("1979-01-01,0", lines[0]);
        Assert.IsTrue(lines.Contains("1981-01-01,0"));
        await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);
    }

    [TestMethod]
    public async Task DownloadAsync_PreviousPublishedFileExists_ReusesStableYearAndOnlyFetchesCurrentYear()
    {
        // 1980-07-14 is day 196 of the (leap) year, so the current year needs at least 186 populated days.
        var now = new DateTimeOffset(1980, 7, 14, 0, 0, 0, TimeSpan.Zero);
        SeedPublishedFile(["1979-01-01,0", "1979-06-15,500"]);
        var handler = new FakeGreenlandHandler(
            new Dictionary<int, string> { [1980] = BuildYearJson(ConsecutiveDays(1980, 200)) });
        var request = await ResolveRequest();

        var artifact = await CreateDownloader(handler, now).DownloadAsync(request, temporaryRoot, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { 1980 }, handler.RequestedYears);
        var lines = await File.ReadAllLinesAsync(artifact.CandidateFilePath);
        CollectionAssert.AreEqual(new[] { "1979-01-01,0", "1979-06-15,500" }, lines.Where(x => x.StartsWith("1979", StringComparison.Ordinal)).ToArray());
        Assert.HasCount(200, lines.Where(x => x.StartsWith("1980", StringComparison.Ordinal)).ToArray());
    }

    [TestMethod]
    public async Task DownloadAsync_JanuaryBoundary_RefetchesPreviousYearAsWellAsCurrentYear()
    {
        // 1981-01-15 is day 15 of the year, so the current year needs at least 5 populated days.
        var now = new DateTimeOffset(1981, 1, 15, 0, 0, 0, TimeSpan.Zero);
        SeedPublishedFile(["1979-01-01,0", "1980-01-01,999"]);
        var handler = new FakeGreenlandHandler(
            new Dictionary<int, string>
            {
                [1980] = BuildYearJson(FullYear(1980, 42)),
                [1981] = BuildYearJson(ConsecutiveDays(1981, 20)),
            });
        var request = await ResolveRequest();

        var artifact = await CreateDownloader(handler, now).DownloadAsync(request, temporaryRoot, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { 1980, 1981 }, handler.RequestedYears);
        var content = await File.ReadAllTextAsync(artifact.CandidateFilePath);
        StringAssert.Contains(content, "1979-01-01,0");
        StringAssert.Contains(content, "1980-01-01,42");
        Assert.IsFalse(content.Contains("1980-01-01,999", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task DownloadAsync_ApiOmitsOrNullsADate_OmitsTheRowInsteadOfWritingZero()
    {
        // 1979-03-10 is day 69 of the year, so the year needs at least 59 populated days.
        var now = new DateTimeOffset(1979, 3, 10, 0, 0, 0, TimeSpan.Zero);
        var entries = Enumerable.Range(1, 70)
            .Where(day => day != 40) // day 40 (1979-02-09) is entirely absent from the API response.
            .Select(day => (
                Date: new DateOnly(1979, 1, 1).AddDays(day - 1),
                Value: day == 41 ? (double?)null : day == 20 ? 0d : 1d)) // day 41 (1979-02-10) is present but null.
            .ToList();
        var handler = new FakeGreenlandHandler(new Dictionary<int, string> { [1979] = BuildYearJson(entries) });
        var request = await ResolveRequest();

        var artifact = await CreateDownloader(handler, now).DownloadAsync(request, temporaryRoot, CancellationToken.None);

        var content = await File.ReadAllTextAsync(artifact.CandidateFilePath);
        StringAssert.Contains(content, "1979-01-01,1");
        StringAssert.Contains(content, "1979-01-20,0"); // A genuine reported zero is written explicitly.
        Assert.DoesNotContain("1979-02-09", content, "A day missing from the API response must not appear as a row.");
        Assert.DoesNotContain("1979-02-10", content, "A day with an explicit null value must not appear as a row.");
    }

    [TestMethod]
    public async Task DownloadAsync_CurrentYearImplausiblyIncomplete_ThrowsAndLeavesNoCandidateFile()
    {
        // 1979-06-01 is day 152 of the year, so the year needs at least 142 populated days; only 5 are supplied.
        var now = new DateTimeOffset(1979, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var handler = new FakeGreenlandHandler(new Dictionary<int, string> { [1979] = BuildYearJson(ConsecutiveDays(1979, 5)) });
        var request = await ResolveRequest();

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => CreateDownloader(handler, now).DownloadAsync(request, temporaryRoot, CancellationToken.None));

        Assert.IsFalse(File.Exists(Path.Combine(temporaryRoot, request.RelativePath)));
    }

    private GreenlandDataSetDownloader CreateDownloader(HttpMessageHandler handler, DateTimeOffset now)
    {
        var httpClient = new HttpClient(handler);
        return new GreenlandDataSetDownloader(
            new GreenlandMeltDataClient(httpClient),
            new DataSetSourceFileStore(sourceRoot),
            new FakeTimeProvider(now));
    }

    private void SeedPublishedFile(IEnumerable<string> lines)
    {
        var path = Path.Combine(sourceRoot, "Greenland", "greenland-melt-area.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
    }

    private static List<(DateOnly Date, double? Value)> ConsecutiveDays(int year, int count, double value = 0)
    {
        var start = new DateOnly(year, 1, 1);
        return Enumerable.Range(0, count).Select(i => (start.AddDays(i), (double?)value)).ToList();
    }

    private static List<(DateOnly Date, double? Value)> FullYear(int year, double value = 0)
    {
        return ConsecutiveDays(year, DateTime.IsLeapYear(year) ? 366 : 365, value);
    }

    private static string BuildYearJson(IEnumerable<(DateOnly Date, double? Value)> entries)
    {
        var dict = new Dictionary<string, double?>();
        foreach (var (date, value) in entries)
        {
            dict[$"{date:yyyy-MM-dd}T00:00:00.000Z"] = value;
        }

        return JsonSerializer.Serialize(dict);
    }

    private static async Task<DataSetDownloadRequest> ResolveRequest()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
        var definition = definitions.Single(x => x.ShortName == "Greenland ice melt");
        var locationId = definition.DataLocationMapping!.LocationIdToDataFileMappings.Single().Key;
        var body = new PostDataSetsRequestBody
        {
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = definition.Id,
                    DataType = DataType.IceMeltArea,
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

    private sealed class FakeGreenlandHandler(IReadOnlyDictionary<int, string> jsonByYear) : HttpMessageHandler
    {
        public List<int> RequestedYears { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            var year = int.Parse(url[(url.LastIndexOf('/') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
            RequestedYears.Add(year);
            if (!jsonByYear.TryGetValue(year, out var json))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
