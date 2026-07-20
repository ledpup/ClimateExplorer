namespace ClimateExplorer.UnitTests;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.WebApi;
using ClimateExplorer.WebApi.AcornSat;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Exercises the ACORN-SAT on-request CDO extension end to end through <c>/climate-record</c> against the
/// real packaged Adelaide ACORN-SAT/CDO data, using a stub CDO source coordinator so no network access is
/// required. Assertions deliberately avoid depending on the exact eligibility outcome or overlay content of
/// today's packaged data - only on the invariants the design guarantees regardless of outcome.
/// </summary>
[TestClass]
public sealed class ClimateRecordsEndpointsAcornSatTests
{
    // Adelaide: adjusted station 023000 throughout; CDO closes 023000 in 1977, opens 023090 until
    // 2018-06-30, then reopens 023000 (see AcornSatStationResolverTests).
    private static readonly Guid AdelaideLocationId = Guid.Parse("70a07bb0-2220-402b-be83-f2c35edfdd12");
    private static readonly DateTimeOffset RetrievedDate = new(2026, 7, 10, 6, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task GetClimateRecords_AcornSatAdjustedTempMaxDaily_ReturnsComposedSeriesWithAcornSatMetadata()
    {
        var response = await ClimateRecordsEndpoints.GetClimateRecords(
            CreateServices(),
            AdelaideLocationId,
            DataType.TempMax,
            DataAdjustment.Adjusted,
            cancellationToken: CancellationToken.None,
            acornSatClimateRecordService: CreateAcornSatService());

        Assert.IsTrue(response.DataResolution.HasValue);
        Assert.IsNotNull(response.SourceMetadata);
        Assert.IsTrue(response.SourceMetadata!.Any(x => x.SourceCode == "ACORN-SAT"));
        AssertCdoMetadataConsistentWithRetrievedDate(response);
    }

    [TestMethod]
    public async Task GetClimateRecords_AcornSatAdjustedTempMaxMonthly_AggregatesComposedSeriesToMonthly()
    {
        var response = await ClimateRecordsEndpoints.GetClimateRecords(
            CreateServices(),
            AdelaideLocationId,
            DataType.TempMax,
            DataAdjustment.Adjusted,
            monthly: true,
            cancellationToken: CancellationToken.None,
            acornSatClimateRecordService: CreateAcornSatService());

        Assert.AreEqual(DataResolution.Monthly, response.DataResolution);
        Assert.IsTrue(response.Records.Count > 0);
    }

    [TestMethod]
    public async Task GetClimateRecords_AcornSatUnadjustedRequest_DoesNotRouteThroughExtension()
    {
        // Only the adjusted measurement triggers the extension; ACORN-SAT itself has no unadjusted
        // measurement, so this should fall back to the ordinary (empty) not-found response rather than
        // throwing from inside the ACORN-SAT service.
        var response = await ClimateRecordsEndpoints.GetClimateRecords(
            CreateServices(),
            AdelaideLocationId,
            DataType.TempMax,
            DataAdjustment.Unadjusted,
            cancellationToken: CancellationToken.None,
            acornSatClimateRecordService: CreateAcornSatService());

        // The unadjusted request resolves to CDO instead of ACORN-SAT, so it must still take the ordinary
        // DataSetEndpoints.PostDataSets path (which the stub coordinator here doesn't service); assert only
        // that no exception was thrown and the ACORN-SAT-specific metadata entry is absent.
        Assert.IsFalse(response.SourceMetadata?.Any(x => x.SourceCode == "ACORN-SAT") ?? false);
    }

    [TestMethod]
    public async Task GetClimateRecords_NoAcornSatServiceRegistered_FallsBackToOrdinaryDataSetPath()
    {
        var response = await ClimateRecordsEndpoints.GetClimateRecords(
            CreateServices(),
            AdelaideLocationId,
            DataType.TempMax,
            DataAdjustment.Adjusted,
            cancellationToken: CancellationToken.None,
            acornSatClimateRecordService: null);

        // With no ACORN-SAT service supplied, the request falls back to DataSetEndpoints.PostDataSets against
        // the stub CDO-oriented coordinator in CreateServices(), which reports RefreshFailed; base ACORN-SAT
        // is still read from the packaged archive so the response should not be empty.
        Assert.IsTrue(response.DataResolution.HasValue);
    }

    private static void AssertCdoMetadataConsistentWithRetrievedDate(ClimateRecordsResponse response)
    {
        var cdoContributed = response.SourceMetadata!.Any(x => x.SourceCode == "BOM-CDO");
        if (cdoContributed)
        {
            Assert.AreEqual(RetrievedDate, response.RetrievedDate);
        }
        else
        {
            Assert.IsNull(response.RetrievedDate);
        }
    }

    private static ClimateExplorerApiServices CreateServices()
    {
        var coordinator = new StubSourceUpdateCoordinator(
            new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed));
        return new ClimateExplorerApiServices(new MemoryCache(), new MemoryCache(), new HttpClient(), new HttpClient(), coordinator);
    }

    private static AcornSatClimateRecordService CreateAcornSatService()
    {
        var coordinator = new StubSourceUpdateCoordinator(
            new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.Rebuild, RetrievedDate));
        return new AcornSatClimateRecordService(
            coordinator,
            new AcornSatExtensionCache(new MemoryCache()),
            new DataSetAssetLockProvider(),
            new FixedTimeProvider(RetrievedDate),
            NullLogger<AcornSatClimateRecordService>.Instance);
    }

    private sealed class StubSourceUpdateCoordinator(DataSetSourcePreparationResult result) : IDataSetSourceUpdateCoordinator
    {
        public Task<DataSetSourcePreparationResult> PrepareAsync(
            PostDataSetsRequestBody request,
            ICachedData? cachedData,
            bool permitSourceUpdate,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
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
