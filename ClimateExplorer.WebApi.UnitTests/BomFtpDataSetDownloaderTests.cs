namespace ClimateExplorer.WebApi.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
public sealed class BomFtpDataSetDownloaderTests
{
    private const string OctoberTablePath = "/anon/gen/clim_data/IDCKWCDEA0/tables/wa/kalumburu/kalumburu-202610.csv";
    private const string SeptemberTablePath = "/anon/gen/clim_data/IDCKWCDEA0/tables/wa/kalumburu/kalumburu-202609.csv";
    private const string AugustTablePath = "/anon/gen/clim_data/IDCKWCDEA0/tables/wa/kalumburu/kalumburu-202608.csv";
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 8, 0, 0, TimeSpan.Zero);
    private string temporaryRoot = null!;
    private string publishedRoot = null!;
    private string workspace = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerBomFtpTests-{Guid.NewGuid():N}");
        publishedRoot = Path.Combine(temporaryRoot, "published");
        workspace = Path.Combine(temporaryRoot, "workspace");
        Directory.CreateDirectory(workspace);
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
    [DataRow("066214", "SYDNEY (OBSERVATORY HILL)", "nsw/sydney_(observatory_hill)")]
    [DataRow("023000", "ADELAIDE (WEST TERRACE / NGAYIRDAPIRA)", "sa/adelaide_(west_terrace___ngayirdapira)")]
    [DataRow("009021", "PERTH AIRPORT M.O.", "wa/perth_airport_mo")]
    [DataRow("015590", "ALICE SPRINGS AIRPORT", "nt/alice_springs_airport")]
    [DataRow("040004", "AMBERLEY AMO", "qld/amberley_amo")]
    [DataRow("082039", "RUTHERGLEN RESEARCH", "vic/rutherglen_research")]
    [DataRow("094029", "HOBART (ELLERSLIE ROAD)", "tas/hobart_(ellerslie_road)")]
    public void GetTableFolder_StationIdAndName_ReturnsBomTableFolder(string stationId, string stationName, string expected)
    {
        Assert.AreEqual(expected, BomFtpDataSetDownloader.GetTableFolder(stationId, stationName));
    }

    [TestMethod]
    public async Task DownloadAsync_TableHasNewerDays_AppendsOnlyNewerDaysInCdoFormat()
    {
        var request = await ResolveStationRequest();
        WritePublishedArchive(lastDate: new DateOnly(2026, 10, 18));
        var tables = new StubTableSource(new Dictionary<string, string> { [OctoberTablePath] = OctoberTable });

        var artifact = await CreateDownloader(tables).DownloadAsync(request, workspace, CancellationToken.None);
        var latestRecordDate = await new DataSetDownloadValidator().ValidateAsync(request, workspace, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { OctoberTablePath }, tables.RequestedPaths);
        using var archive = ZipFile.OpenRead(artifact.CandidateFilePath);
        CollectionAssert.AreEqual(
            new[] { "IDCJAC0010,001019,2026,10,18,30.0,1,Y", "IDCJAC0010,001019,2026,10,19,33.1,1,N", "IDCJAC0010,001019,2026,10,20,34.5,1,N" },
            ReadEntry(archive, "001019_daily_tempmax.csv").TakeLast(3).ToArray());
        CollectionAssert.AreEqual(
            new[] { "IDCJAC0011,001019,2026,10,18,20.0,1,Y", "IDCJAC0011,001019,2026,10,19,21.4,1,N" },
            ReadEntry(archive, "001019_daily_tempmin.csv").TakeLast(2).ToArray());
        Assert.AreEqual("IDCJAC0009,001019,2026,10,20,0.0,1,N", ReadEntry(archive, "001019_daily_rainfall.csv").Last());
        Assert.AreEqual("IDCJAC0016,001019,2026,10,20,26.4", ReadEntry(archive, "001019_daily_solarradiation.csv").Last());
        Assert.AreEqual("20261019,27.25", ReadEntry(archive, "001019_daily_tempmean.csv").Last());
        Assert.AreEqual(new DateOnly(2026, 10, 19), latestRecordDate);
    }

    [TestMethod]
    public async Task DownloadAsync_NoTableForStation_ThrowsInvalidDataException()
    {
        var request = await ResolveStationRequest();
        WritePublishedArchive(lastDate: new DateOnly(2026, 10, 18));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => CreateDownloader(new StubTableSource([])).DownloadAsync(request, workspace, CancellationToken.None));
    }

    [TestMethod]
    public async Task DownloadAsync_EarlierMonthTableMissing_ThrowsInvalidDataException()
    {
        var request = await ResolveStationRequest();
        WritePublishedArchive(lastDate: new DateOnly(2026, 8, 20));
        var tables = new StubTableSource(new Dictionary<string, string>
        {
            [AugustTablePath] = OctoberTable.Replace("/10/2026", "/08/2026", StringComparison.Ordinal),
            [OctoberTablePath] = OctoberTable,
        });

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => CreateDownloader(tables).DownloadAsync(request, workspace, CancellationToken.None));
    }

    [TestMethod]
    public async Task DownloadAsync_NewestMonthTableNotPublishedYet_AppendsEarlierMonths()
    {
        var request = await ResolveStationRequest();
        WritePublishedArchive(lastDate: new DateOnly(2026, 9, 17));
        var tables = new StubTableSource(new Dictionary<string, string>
        {
            [SeptemberTablePath] = OctoberTable.Replace("/10/2026", "/09/2026", StringComparison.Ordinal),
        });

        var artifact = await CreateDownloader(tables).DownloadAsync(request, workspace, CancellationToken.None);

        using var archive = ZipFile.OpenRead(artifact.CandidateFilePath);
        Assert.AreEqual("IDCJAC0010,001019,2026,09,20,34.5,1,N", ReadEntry(archive, "001019_daily_tempmax.csv").Last());
    }

    [TestMethod]
    public async Task DownloadAsync_TableColumnsChanged_ThrowsInvalidDataException()
    {
        var request = await ResolveStationRequest();
        WritePublishedArchive(lastDate: new DateOnly(2026, 10, 18));
        var changedTable = OctoberTable.Replace("Maximum,Minimum", "Minimum,Maximum", StringComparison.Ordinal);
        var tables = new StubTableSource(new Dictionary<string, string> { [OctoberTablePath] = changedTable });

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => CreateDownloader(tables).DownloadAsync(request, workspace, CancellationToken.None));
    }

    private const string OctoberTable =
        "Daily Evapotranspiration for KALUMBURU Western Australia for October 2026,,,,,,,,,,\n" +
        ",,Evapo-,,Pan,,,Maximum,Minimum,Average,\n" +
        ",,Transpiration,Rain,Evaporation,Maximum,Minimum,Relative,Relative,10m Wind,Solar\n" +
        "Station Name,Date,0000-2400,0900-0900,0900-0900,Temperature,Temperature,Humidity,Humidity,Speed,Radiation\n" +
        "KALUMBURU,18/10/2026,6.0,0.0, ,31.0,21.0,90,30,3.00,25.00\n" +
        "KALUMBURU,19/10/2026,6.2,1.2, ,33.1,21.4,88,31,2.90,24.81\n" +
        "KALUMBURU,20/10/2026,6.4,0.0, ,34.5, ,85,29,3.10,26.37\n" +
        "Totals:,,18.6,1.2, ,,,,,,\n";

    private BomFtpDataSetDownloader CreateDownloader(IBomDailyTableSource tables)
    {
        return new BomFtpDataSetDownloader(
            tables,
            new DataSetSourceFileStore(publishedRoot),
            new FixedTimeProvider(Now),
            Path.Combine(Folders.MetaDataFolder, "Station"));
    }

    private void WritePublishedArchive(DateOnly lastDate)
    {
        var dates = new[] { lastDate.AddDays(-1), lastDate };
        string BomRows(string productCode, string value, bool hasQualityColumns) => string.Join(
            "\n",
            dates.Select(x => $"{productCode},001019,{x:yyyy},{x:MM},{x:dd},{value}" + (hasQualityColumns ? ",1,Y" : string.Empty)));

        var entries = new Dictionary<string, string>
        {
            ["001019_daily_tempmean.csv"] = string.Join("\n", dates.Select(x => $"{x:yyyyMMdd},25")),
            ["001019_daily_tempmax.csv"] = BomRows("IDCJAC0010", "30.0", hasQualityColumns: true),
            ["001019_daily_tempmin.csv"] = BomRows("IDCJAC0011", "20.0", hasQualityColumns: true),
            ["001019_daily_rainfall.csv"] = BomRows("IDCJAC0009", "0.0", hasQualityColumns: true),
            ["001019_daily_solarradiation.csv"] = BomRows("IDCJAC0016", "25.0", hasQualityColumns: false),
        };

        var archivePath = Path.Combine(publishedRoot, "BOM", "001019.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open());
            writer.Write(content);
        }
    }

    private static string[] ReadEntry(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open());
        return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    private static async Task<DataSetDownloadRequest> ResolveStationRequest()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
        var definition = definitions.Single(x => x.ShortName == "BOM-CDO");
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

    private sealed class StubTableSource(Dictionary<string, string> tables) : IBomDailyTableSource
    {
        public List<string> RequestedPaths { get; } = [];

        public Task<string?> DownloadAsync(string remotePath, CancellationToken cancellationToken)
        {
            RequestedPaths.Add(remotePath);
            return Task.FromResult(tables.GetValueOrDefault(remotePath));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
