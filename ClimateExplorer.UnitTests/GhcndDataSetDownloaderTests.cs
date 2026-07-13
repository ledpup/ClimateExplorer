namespace ClimateExplorer.UnitTests;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading;
using ClimateExplorer.Data.Ghcnd;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class GhcndDataSetDownloaderTests
{
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerGhcndDownloadTests-{Guid.NewGuid():N}");
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
    public async Task DownloadAsync_SharedStationMapping_CreatesAndValidatesBothArchiveEntries()
    {
        var request = await ResolveSharedStationRequest();
        using var httpClient = new HttpClient(new CsvHttpMessageHandler(CreateStationCsv()));
        var downloader = new GhcndDataSetDownloader(httpClient);

        var artifact = await downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None);
        await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);

        using var archive = ZipFile.OpenRead(artifact.CandidateFilePath);
        CollectionAssert.AreEquivalent(
            new[] { "Temperature/AE000041196.csv", "Precipitation/AE000041196.csv" },
            archive.Entries.Select(x => x.FullName).ToArray());
    }

    [TestMethod]
    public async Task BuildAsync_TemperatureOnly_CreatesOnlyTemperatureEntry()
    {
        var archivePath = Path.Combine(temporaryRoot, "temperature.zip");

        await GhcndStationArchiveBuilder.BuildAsync(
            CreateStationCsv(),
            "TEST0000001",
            archivePath,
            includeTemperature: true,
            includePrecipitation: false,
            CancellationToken.None);

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.HasCount(1, archive.Entries);
        Assert.AreEqual("Temperature/TEST0000001.csv", archive.Entries[0].FullName);
    }

    [TestMethod]
    public async Task BuildAsync_PrecipitationOnly_CreatesOnlyPrecipitationEntry()
    {
        var archivePath = Path.Combine(temporaryRoot, "precipitation.zip");

        await GhcndStationArchiveBuilder.BuildAsync(
            CreateStationCsv(),
            "TEST0000001",
            archivePath,
            includeTemperature: false,
            includePrecipitation: true,
            CancellationToken.None);

        using var archive = ZipFile.OpenRead(archivePath);
        Assert.HasCount(1, archive.Entries);
        Assert.AreEqual("Precipitation/TEST0000001.csv", archive.Entries[0].FullName);
    }

    [TestMethod]
    public async Task BuildAsync_InsufficientRows_ThrowsInvalidDataException()
    {
        var archivePath = Path.Combine(temporaryRoot, "invalid.zip");
        var csv = "STATION,DATE,PRCP,PRCP_ATTRIBUTES,TMAX,TMAX_ATTRIBUTES,TMIN,TMIN_ATTRIBUTES\nTEST,2024-01-01,10,,200,,100,";

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => GhcndStationArchiveBuilder.BuildAsync(
                csv,
                "TEST",
                archivePath,
                includeTemperature: true,
                includePrecipitation: true,
                CancellationToken.None));
    }

    private static async Task<DataSetDownloadRequest> ResolveSharedStationRequest()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
        var definition = definitions.Single(x => x.ShortName == "GHCNd");
        var locationId = definition.DataLocationMapping!.LocationIdToDataFileMappings
            .Single(x => x.Value.Single().Id == "AE000041196").Key;
        var body = new PostDataSetsRequestBody
        {
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = definition.Id,
                    DataType = DataType.TempMax,
                    DataAdjustment = DataAdjustment.Unadjusted,
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

    private static string CreateStationCsv()
    {
        var csv = new StringBuilder("STATION,DATE,PRCP,PRCP_ATTRIBUTES,TMAX,TMAX_ATTRIBUTES,TMIN,TMIN_ATTRIBUTES\n");
        var date = new DateOnly(2024, 1, 1);
        for (var i = 0; i < 301; i++)
        {
            csv.Append("TEST,")
                .Append(date.AddDays(i).ToString("yyyy-MM-dd"))
                .Append(",10,,200,,100,\n");
        }

        return csv.ToString();
    }

    private sealed class CsvHttpMessageHandler(string csv) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(csv),
            });
        }
    }
}
