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
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class BomDataSetDownloaderTests
{
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerBomDownloadTests-{Guid.NewGuid():N}");
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
    public async Task DownloadAsync_CompleteProviderResponses_CreatesAndValidatesFiveEntryArchive()
    {
        var request = await ResolveStationRequest();
        using var httpClient = new HttpClient(new BomHttpMessageHandler());
        var downloader = new BomDataSetDownloader(new BomDailyDataClient(httpClient));

        var artifact = await downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None);
        await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);

        using var archive = ZipFile.OpenRead(artifact.CandidateFilePath);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "001019_daily_tempmean.csv",
                "001019_daily_tempmax.csv",
                "001019_daily_tempmin.csv",
                "001019_daily_rainfall.csv",
                "001019_daily_solarradiation.csv",
            },
            archive.Entries.Select(x => x.FullName).ToArray());
        using var reader = new StreamReader(archive.GetEntry("001019_daily_tempmean.csv")!.Open());
        Assert.AreEqual("20240101,25", await reader.ReadLineAsync());
    }

    [TestMethod]
    public async Task DownloadAsync_OneProviderDownloadFails_DoesNotCreateCandidateArchive()
    {
        var request = await ResolveStationRequest();
        using var httpClient = new HttpClient(new BomHttpMessageHandler(BomDailyObservationCode.SolarRadiation));
        var downloader = new BomDataSetDownloader(new BomDailyDataClient(httpClient));

        await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None));

        Assert.IsFalse(File.Exists(Path.Combine(temporaryRoot, "BOM", "001019.zip")));
    }

    [TestMethod]
    public async Task DownloadAsync_CorruptProviderArchive_ThrowsInvalidDataException()
    {
        var request = await ResolveStationRequest();
        using var httpClient = new HttpClient(new BomHttpMessageHandler(corruptArchive: true));
        var downloader = new BomDataSetDownloader(new BomDailyDataClient(httpClient));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => downloader.DownloadAsync(request, temporaryRoot, CancellationToken.None));
    }

    private static async Task<DataSetDownloadRequest> ResolveStationRequest()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
        var definition = definitions.Single(x => x.ShortName == "BOM");
        var locationId = definition.DataLocationMapping!.LocationIdToDataFileMappings
            .First(x => x.Value.Any(y => y.Id == "001019")).Key;
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

        var assets = await new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"))
            .ResolveAsync(body, CancellationToken.None);
        return assets.Single(x => x.RelativePath == @"BOM\001019.zip");
    }

    private sealed class BomHttpMessageHandler : HttpMessageHandler
    {
        private readonly BomDailyObservationCode? failingCode;
        private readonly bool corruptArchive;

        public BomHttpMessageHandler(BomDailyObservationCode? failingCode = null, bool corruptArchive = false)
        {
            this.failingCode = failingCode;
            this.corruptArchive = corruptArchive;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("availableYears", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("001019||,1990:77,"),
                });
            }

            var observationCode = Enum.GetValues<BomDailyObservationCode>()
                .Single(x => url.Contains($"p_nccObsCode={(int)x}", StringComparison.Ordinal));
            if (observationCode == failingCode)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            var content = corruptArchive ? Encoding.UTF8.GetBytes("not a zip") : CreateZip(GetCsv(observationCode));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content),
            });
        }

        private static string GetCsv(BomDailyObservationCode observationCode)
        {
            return observationCode switch
            {
                BomDailyObservationCode.TemperatureMaximum => "Product code,Bureau of Meteorology station number,Year,Month,Day,Maximum temperature (Degree C),Days,Quality\nIDCJAC0010,001019,2024,01,01,30,,\nIDCJAC0010,001019,2024,01,02,32,,",
                BomDailyObservationCode.TemperatureMinimum => "Product code,Bureau of Meteorology station number,Year,Month,Day,Minimum temperature (Degree C),Days,Quality\nIDCJAC0011,001019,2024,01,01,20,,\nIDCJAC0011,001019,2024,01,02,22,,",
                BomDailyObservationCode.Rainfall => "Product code,Bureau of Meteorology station number,Year,Month,Day,Rainfall amount (millimetres),Days,Quality\nIDCJAC0009,001019,2024,01,01,10,,\nIDCJAC0009,001019,2024,01,02,11,,",
                BomDailyObservationCode.SolarRadiation => "Product code,Bureau of Meteorology station number,Year,Month,Day,Daily global solar exposure (MJ/m*m)\nIDCJAC0016,001019,2024,01,01,15\nIDCJAC0016,001019,2024,01,02,16",
                _ => throw new ArgumentOutOfRangeException(nameof(observationCode)),
            };
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
