namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

    private DataSetSourceUpdateCoordinator CreateCoordinator(IDataSetDownloader downloader)
    {
        var timeProvider = new FixedTimeProvider(Now);
        return new DataSetSourceUpdateCoordinator(
            new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping")),
            new DataSetFreshnessPolicy(timeProvider),
            new DataSetAssetLockProvider(),
            new DataSetDownloadWorkspaceFactory(),
            new DataSetSourceFileStore(temporaryRoot),
            new MemoryStateStore(),
            new DataSetDownloadValidator(),
            [downloader],
            timeProvider,
            NullLogger<DataSetSourceUpdateCoordinator>.Instance);
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

    private const string ValidNinoContent = "2025 1.0 1.1 1.2 1.3 1.4 1.5 1.6 1.7 1.8 1.9 2.0 2.1";

    private sealed class StubDownloader(string content, TimeSpan? delay = null) : IDataSetDownloader
    {
        private int callCount;

        public string Key => "direct-http";

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
        public override DateTimeOffset GetUtcNow() => now;
    }
}
