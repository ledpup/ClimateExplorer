namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Concurrent;
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
using ClimateExplorer.Data.Downloading.Workspace;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class DataSetSourceUpdateCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerSourceCoordinatorTests-{Guid.NewGuid():N}");
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
    public async Task PrepareAsync_ColdManagedSource_DownloadsValidatesPublishesAndReturnsRetrievalDate()
    {
        var downloader = new StubDownloader(ValidNinoContent);
        var coordinator = CreateCoordinator(downloader);
        var request = await CreateNinoRequest();

        var result = await coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.Rebuild, result.Outcome);
        Assert.AreEqual(Now, result.RetrievedDate);
        Assert.AreEqual(1, downloader.CallCount);
        Assert.AreEqual(ValidNinoContent, await File.ReadAllTextAsync(Path.Combine(temporaryRoot, "Nino34", "nino34.long.anom.data.txt")));
    }

    [TestMethod]
    public async Task PrepareAsync_ColdManagedSource_RecordsHashOfPublishedContent()
    {
        var stateStore = new MemoryStateStore();
        var coordinator = CreateCoordinator(new StubDownloader(ValidNinoContent), stateStore: stateStore);

        await coordinator.PrepareAsync(await CreateNinoRequest(), null, permitSourceUpdate: true, CancellationToken.None);

        var published = await DataSetSourceFileStore.ComputeFileInfoAsync(Path.Combine(temporaryRoot, "Nino34", "nino34.long.anom.data.txt"), CancellationToken.None);
        var state = await stateStore.GetAsync("NINO34/NINO34.LONG.ANOM.DATA.TXT", CancellationToken.None);
        Assert.AreEqual(published.Sha256, state!.Sha256);
        Assert.AreEqual(published.Length, state.Length);
    }

    [TestMethod]
    public async Task PrepareAsync_ColdManagedSourceWithoutPermitSourceUpdate_DoesNotDownloadAndReturnsRefreshFailed()
    {
        var downloader = new StubDownloader(ValidNinoContent);
        var coordinator = CreateCoordinator(downloader);
        var request = await CreateNinoRequest();

        var result = await coordinator.PrepareAsync(request, null, permitSourceUpdate: false, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.RefreshFailed, result.Outcome);
        Assert.AreEqual(0, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_FreshResponseAndMatchingSourceState_ReturnsCacheWithoutDownloadingAgain()
    {
        var downloader = new StubDownloader(ValidNinoContent);
        var coordinator = CreateCoordinator(downloader);
        var request = await CreateNinoRequest();
        await coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None);
        var cachedData = new DataSet { RetrievedDate = Now };

        var result = await coordinator.PrepareAsync(request, cachedData, permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.UseCached, result.Outcome);
        Assert.AreEqual(1, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_InvalidDownload_PreservesExistingSourceAndReturnsRefreshFailed()
    {
        var sourcePath = Path.Combine(temporaryRoot, "Nino34", "nino34.long.anom.data.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        await File.WriteAllTextAsync(sourcePath, ValidNinoContent);
        var downloader = new StubDownloader("not a valid dataset");
        var coordinator = CreateCoordinator(downloader);

        var result = await coordinator.PrepareAsync(await CreateNinoRequest(), new DataSet(), permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.RefreshFailed, result.Outcome);
        Assert.AreEqual(ValidNinoContent, await File.ReadAllTextAsync(sourcePath));
    }

    [TestMethod]
    public async Task PrepareAsync_ConcurrentRequestsForSameAsset_DownloadsOnce()
    {
        var downloader = new StubDownloader(ValidNinoContent, TimeSpan.FromMilliseconds(100));
        var coordinator = CreateCoordinator(downloader);
        var request = await CreateNinoRequest();

        var results = await Task.WhenAll(
            coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None),
            coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None));

        Assert.IsTrue(results.All(x => x.Outcome == DataSetSourcePreparationOutcome.Rebuild));
        Assert.AreEqual(1, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_DownloadExceedsRefreshTimeout_ReturnsRefreshFailed()
    {
        var downloader = new StubDownloader(ValidNinoContent, TimeSpan.FromSeconds(30));
        var coordinator = CreateCoordinator(downloader, onRequestRefreshTimeout: TimeSpan.FromMilliseconds(100));

        var result = await coordinator.PrepareAsync(await CreateNinoRequest(), new DataSet(), permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.RefreshFailed, result.Outcome);
        Assert.AreEqual(1, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_AssetLockHeldBeyondRefreshTimeout_ReturnsRefreshFailedWithoutDownloading()
    {
        var downloader = new StubDownloader(ValidNinoContent);
        var lockProvider = new DataSetAssetLockProvider();
        var coordinator = CreateCoordinator(downloader, lockProvider, TimeSpan.FromMilliseconds(100));
        var request = await CreateNinoRequest();
        await using var heldLock = await lockProvider.AcquireAsync("NINO34/NINO34.LONG.ANOM.DATA.TXT", CancellationToken.None);

        var result = await coordinator.PrepareAsync(request, new DataSet(), permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.RefreshFailed, result.Outcome);
        Assert.AreEqual(0, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_CallerCancelsDuringDownload_ThrowsOperationCanceled()
    {
        var downloader = new StubDownloader(ValidNinoContent, TimeSpan.FromSeconds(30));
        var coordinator = CreateCoordinator(downloader);
        var request = await CreateNinoRequest();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => coordinator.PrepareAsync(request, new DataSet(), permitSourceUpdate: true, cancellation.Token));
    }

    [TestMethod]
    public async Task PrepareAsync_PublishedBomSourceHasYesterdaysRecordButNoState_AdoptsWithoutDownloading()
    {
        var downloader = new StubDownloader(string.Empty, key: "bom-station");
        var coordinator = CreateCoordinator(downloader);
        var (request, stationId) = await CreateBomRequest();
        WriteBomArchive(stationId, lastDate: DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1));

        var result = await coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.Rebuild, result.Outcome);
        Assert.AreEqual(0, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_PublishedBomSourceAdopted_RecordsHashAndDateOfPublishedFile()
    {
        var yesterday = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1);
        var stateStore = new MemoryStateStore();
        var coordinator = CreateCoordinator(new StubDownloader(string.Empty, key: "bom-station"), stateStore: stateStore);
        var (request, stationId) = await CreateBomRequest();
        WriteBomArchive(stationId, lastDate: yesterday);

        await coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None);

        var published = await DataSetSourceFileStore.ComputeFileInfoAsync(Path.Combine(temporaryRoot, "BOM", $"{stationId}.zip"), CancellationToken.None);
        var state = await stateStore.GetAsync($"BOM/{stationId}.ZIP", CancellationToken.None);
        Assert.AreEqual(published.Sha256, state!.Sha256);
        Assert.AreEqual(yesterday, state.LatestRecordDate);
        Assert.AreEqual(published.LastWriteTimeUtc, state.RetrievedDate);
    }

    [TestMethod]
    public async Task PrepareAsync_FreshResponseAndCurrentBomSource_ReturnsSourceLatestRecordDate()
    {
        var yesterday = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1);
        var coordinator = CreateCoordinator(new StubDownloader(string.Empty, key: "bom-station"));
        var (request, stationId) = await CreateBomRequest();
        WriteBomArchive(stationId, lastDate: yesterday);
        var rebuild = await coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None);

        var result = await coordinator.PrepareAsync(request, new DataSet { RetrievedDate = Now }, permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(yesterday, rebuild.LatestRecordDate);
        Assert.AreEqual(DataSetSourcePreparationOutcome.UseCached, result.Outcome);
        Assert.AreEqual(yesterday, result.LatestRecordDate);
    }

    [TestMethod]
    public async Task PrepareAsync_PublishedBomSourceLacksYesterdaysRecordAndNoState_Downloads()
    {
        var downloader = new StubDownloader(string.Empty, key: "bom-station");
        var coordinator = CreateCoordinator(downloader);
        var (request, stationId) = await CreateBomRequest();
        WriteBomArchive(stationId, lastDate: DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-3));

        await coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(1, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_RefreshFailedRecently_SkipsDownloadUntilBackoffExpires()
    {
        var downloader = new StubDownloader("not a valid dataset");
        var timeProvider = new FixedTimeProvider(Now);
        var coordinator = CreateCoordinator(downloader, timeProvider: timeProvider);
        var request = await CreateNinoRequest();

        var first = await coordinator.PrepareAsync(request, new DataSet(), permitSourceUpdate: true, CancellationToken.None);
        var duringBackoff = await coordinator.PrepareAsync(request, new DataSet(), permitSourceUpdate: true, CancellationToken.None);
        timeProvider.Now = Now.Add(DataSetSourceUpdateCoordinator.RefreshFailureBackoff);
        var afterBackoff = await coordinator.PrepareAsync(request, new DataSet(), permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.RefreshFailed, first.Outcome);
        Assert.AreEqual(DataSetSourcePreparationOutcome.RefreshFailed, duringBackoff.Outcome);
        Assert.AreEqual(DataSetSourcePreparationOutcome.RefreshFailed, afterBackoff.Outcome);
        Assert.AreEqual(2, downloader.CallCount);
    }

    [TestMethod]
    public async Task PrepareAsync_RefreshNotPermittedThenPermitted_DownloadsWithoutBackoff()
    {
        var downloader = new StubDownloader(ValidNinoContent);
        var coordinator = CreateCoordinator(downloader);
        var request = await CreateNinoRequest();

        await coordinator.PrepareAsync(request, null, permitSourceUpdate: false, CancellationToken.None);
        var result = await coordinator.PrepareAsync(request, null, permitSourceUpdate: true, CancellationToken.None);

        Assert.AreEqual(DataSetSourcePreparationOutcome.Rebuild, result.Outcome);
        Assert.AreEqual(1, downloader.CallCount);
    }

    [TestMethod]
    public async Task TryAcquireAsync_LockHeldBeyondTimeout_ReturnsNull()
    {
        var lockProvider = new DataSetAssetLockProvider();
        await using var heldLock = await lockProvider.AcquireAsync("asset", CancellationToken.None);

        var lease = await lockProvider.TryAcquireAsync("asset", TimeSpan.FromMilliseconds(50), CancellationToken.None);

        Assert.IsNull(lease);
    }

    private DataSetSourceUpdateCoordinator CreateCoordinator(
        IDataSetDownloader downloader,
        DataSetAssetLockProvider? lockProvider = null,
        TimeSpan? onRequestRefreshTimeout = null,
        TimeProvider? timeProvider = null,
        IDataSetSourceStateStore? stateStore = null)
    {
        timeProvider ??= new FixedTimeProvider(Now);
        return new DataSetSourceUpdateCoordinator(
            new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping")),
            new DataSetFreshnessPolicy(timeProvider),
            lockProvider ?? new DataSetAssetLockProvider(),
            new DataSetDownloadWorkspaceFactory(),
            new DataSetSourceFileStore(temporaryRoot),
            stateStore ?? new MemoryStateStore(),
            new DataSetDownloadValidator(),
            [downloader],
            timeProvider,
            NullLogger<DataSetSourceUpdateCoordinator>.Instance,
            onRequestRefreshTimeout);
    }

    private static async Task<PostDataSetsRequestBody> CreateNinoRequest()
    {
        var definition = (await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping")))
            .Single(x => x.ShortName == "Niño 3.4");
        return new PostDataSetsRequestBody
        {
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = definition.Id,
                    DataType = DataType.Nino34,
                    LocationId = definition.DataLocationMapping!.LocationIdToDataFileMappings.Keys.Single(),
                },
            ],
            BinningRule = BinGranularities.ByYearAndMonth,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = 1,
        };
    }

    private static async Task<(PostDataSetsRequestBody Request, string StationId)> CreateBomRequest()
    {
        var definition = (await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping")))
            .Single(x => x.ShortName == "BOM-CDO");
        var (locationId, filters) = definition.DataLocationMapping!.LocationIdToDataFileMappings
            .First(x => x.Value.Count == 1 && x.Value[0].EndDate is null);
        var request = new PostDataSetsRequestBody
        {
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = definition.Id,
                    DataType = DataType.TempMax,
                    LocationId = locationId,
                    DataAdjustment = DataAdjustment.Unadjusted,
                },
            ],
            BinningRule = BinGranularities.ByYearAndDay,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = 1,
        };
        return (request, filters[0].Id);
    }

    private void WriteBomArchive(string stationId, DateOnly lastDate)
    {
        var dates = new[] { lastDate.AddDays(-1), lastDate };
        string BomRows(string productCode, bool hasQualityColumns) => string.Join(
            Environment.NewLine,
            dates.Select(x => $"{productCode},{stationId},{x:yyyy},{x:MM},{x:dd},15.0" + (hasQualityColumns ? ",1,N" : string.Empty)));

        var entries = new Dictionary<string, string>
        {
            ["tempmean"] = string.Join(Environment.NewLine, dates.Select(x => $"{x:yyyyMMdd},15.0")),
            ["tempmax"] = BomRows("IDCJAC0010", hasQualityColumns: true),
            ["tempmin"] = BomRows("IDCJAC0011", hasQualityColumns: true),
            ["rainfall"] = BomRows("IDCJAC0009", hasQualityColumns: true),
            ["solarradiation"] = BomRows("IDCJAC0016", hasQualityColumns: false),
        };

        var archivePath = Path.Combine(temporaryRoot, "BOM", $"{stationId}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var writer = new StreamWriter(archive.CreateEntry($"{stationId}_daily_{name}.csv").Open());
            writer.Write(content);
        }
    }

    private const string ValidNinoContent = "2025 1.0 1.1 1.2 1.3 1.4 1.5 1.6 1.7 1.8 1.9 2.0 2.1";

    private sealed class StubDownloader(string content, TimeSpan? delay = null, string key = "direct-http") : IDataSetDownloader
    {
        private int callCount;

        public string Key => key;

        public int CallCount => callCount;

        public async Task<DataSetDownloadArtifact> DownloadAsync(
            DataSetDownloadRequest request,
            string temporaryDirectory,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            if (delay.HasValue)
            {
                await Task.Delay(delay.Value, cancellationToken);
            }

            var path = Path.Combine(temporaryDirectory, request.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, content, cancellationToken);
            return new DataSetDownloadArtifact(path);
        }
    }

    private sealed class MemoryStateStore : IDataSetSourceStateStore
    {
        private readonly ConcurrentDictionary<string, DataSetSourceState> states = new(StringComparer.Ordinal);

        public Task<DataSetSourceState?> GetAsync(string assetKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(states.TryGetValue(assetKey, out var state) ? state : null);
        }

        public Task PutAsync(DataSetSourceState state, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            states[state.AssetKey] = state;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
