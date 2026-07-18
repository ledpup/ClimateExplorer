namespace ClimateExplorer.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Interface;
using ClimateExplorer.Data.Downloading.Extenders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.WebApi.AcornSat;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class AcornSatClimateRecordServiceTests
{
    // Adelaide: adjusted station 023000 throughout; CDO closes 023000 in 1977, opens 023090 until
    // 2018-06-30, then reopens 023000 (see AcornSatStationResolverTests). Both packaged ACORN-SAT.zip and
    // BOM/023000.zip exist in the repo, so this location can be read end-to-end without network access.
    private static readonly Guid AdelaideLocationId = Guid.Parse("70a07bb0-2220-402b-be83-f2c35edfdd12");
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task ResolveAsync_LocationWithNoOpenCdoStation_ReturnsNotEligibleWithoutCallingCoordinator()
    {
        var coordinator = new StubCoordinator(new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.Rebuild));
        var service = CreateService(coordinator, new MemoryCache());

        var outcome = await service.ResolveAsync(Guid.NewGuid(), DataType.TempMax, CancellationToken.None);

        Assert.AreEqual(AcornSatExtensionDecision.NoOpenCdoStation, outcome.Extension.Decision);
        Assert.AreEqual(0, coordinator.CallCount);
        Assert.IsNull(outcome.RetrievedDate);
    }

    [TestMethod]
    public async Task ResolveAsync_RefreshFailedWithConclusiveCachedEntry_ReturnsCachedOverlayWithoutRereading()
    {
        var cache = new MemoryCache();
        var extensionCache = new AcornSatExtensionCache(cache);
        var cachedEntry = new AcornSatExtensionCacheEntry
        {
            LocationId = AdelaideLocationId,
            DataType = DataType.TempMax,
            AdjustedStationId = "023000",
            OpenCdoStationId = "023000",
            ComparisonYear = 2025,
            Decision = AcornSatExtensionDecision.Eligible,
            LatestAcornSatDate = new DateOnly(2025, 12, 31),
            ComparisonSignature = "cached-signature",
            OverlayRecords = [new ClimateExplorer.Core.Model.DataRecord(new DateOnly(2026, 1, 1), 21.3)],
            RetrievedDate = new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero),
        };
        await extensionCache.PutAsync(cachedEntry);

        var coordinator = new StubCoordinator(new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed));
        var service = CreateService(coordinator, cache);

        var outcome = await service.ResolveAsync(AdelaideLocationId, DataType.TempMax, CancellationToken.None);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, outcome.Extension.Decision);
        Assert.AreEqual("cached-signature", outcome.Extension.ComparisonSignature);
        Assert.AreEqual(1, outcome.Extension.OverlayRecords.Count);
        Assert.AreEqual(cachedEntry.RetrievedDate, outcome.RetrievedDate);
        Assert.AreEqual(1, coordinator.CallCount, "The coordinator should still be asked once, to discover the refresh failure.");
    }

    [TestMethod]
    public async Task ResolveAsync_RefreshFailedWithNoCachedEntry_FallsBackToColdComparisonWithNullRetrievedDate()
    {
        var coordinator = new StubCoordinator(new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed));
        var service = CreateService(coordinator, new MemoryCache());

        var outcome = await service.ResolveAsync(AdelaideLocationId, DataType.TempMax, CancellationToken.None);

        Assert.IsNull(outcome.RetrievedDate);
        Assert.IsTrue(Enum.IsDefined(outcome.Extension.Decision));
    }

    [TestMethod]
    public async Task ResolveAsync_RebuildOutcome_UsesCoordinatorRetrievedDateAndCachesConclusiveDecision()
    {
        var retrievedDate = new DateTimeOffset(2026, 7, 10, 6, 0, 0, TimeSpan.Zero);
        var coordinator = new StubCoordinator(new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.Rebuild, retrievedDate));
        var cache = new MemoryCache();
        var extensionCache = new AcornSatExtensionCache(cache);
        var service = CreateService(coordinator, cache);

        var outcome = await service.ResolveAsync(AdelaideLocationId, DataType.TempMax, CancellationToken.None);

        Assert.IsTrue(Enum.IsDefined(outcome.Extension.Decision));
        if (outcome.Extension.Decision is AcornSatExtensionDecision.Eligible or AcornSatExtensionDecision.AdjustmentsDetected)
        {
            Assert.AreEqual(retrievedDate, outcome.RetrievedDate);
            var stored = await extensionCache.GetAsync(AdelaideLocationId, DataType.TempMax);
            Assert.IsNotNull(stored);
            Assert.AreEqual(outcome.Extension.Decision, stored!.Decision);
        }
    }

    [TestMethod]
    public async Task ResolveAsync_CallerCancelsRequest_PropagatesOperationCanceledException()
    {
        var coordinator = new CancellingCoordinator();
        var service = CreateService(coordinator, new MemoryCache());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // The keyed extension lock (a SemaphoreSlim.WaitAsync) throws TaskCanceledException, a subclass of
        // OperationCanceledException; either way, cancellation must propagate rather than being swallowed.
        try
        {
            await service.ResolveAsync(AdelaideLocationId, DataType.TempMax, cts.Token);
            Assert.Fail("Expected an OperationCanceledException.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static AcornSatClimateRecordService CreateService(IDataSetSourceUpdateCoordinator coordinator, ICache cache)
    {
        return new AcornSatClimateRecordService(
            coordinator,
            new AcornSatExtensionCache(cache),
            new DataSetAssetLockProvider(),
            new FixedTimeProvider(Now),
            NullLogger<AcornSatClimateRecordService>.Instance);
    }

    private sealed class StubCoordinator(DataSetSourcePreparationResult result) : IDataSetSourceUpdateCoordinator
    {
        public int CallCount { get; private set; }

        public Task<DataSetSourcePreparationResult> PrepareAsync(
            PostDataSetsRequestBody request,
            ICachedData? cachedData,
            bool permitSourceUpdate,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class CancellingCoordinator : IDataSetSourceUpdateCoordinator
    {
        public Task<DataSetSourcePreparationResult> PrepareAsync(
            PostDataSetsRequestBody request,
            ICachedData? cachedData,
            bool permitSourceUpdate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Expected cancellation to be observed before this point.");
        }
    }

    private sealed class MemoryCache : ICache
    {
        private readonly System.Collections.Generic.Dictionary<string, object?> values = [];

        public Task<T> Get<T>(string key)
        {
            return Task.FromResult(values.TryGetValue(key, out var value) ? (T)value! : default!);
        }

        public Task Put<T>(string key, T obj)
        {
            values[key] = obj;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
