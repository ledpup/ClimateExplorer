namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Extenders;
using ClimateExplorer.WebApi.AcornSat;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class AcornSatExtensionCacheTests
{
    private static readonly Guid LocationId = Guid.Parse("70a07bb0-2220-402b-be83-f2c35edfdd12");

    [TestMethod]
    public async Task GetAsync_NoEntryCached_ReturnsNull()
    {
        var cache = new AcornSatExtensionCache(new MemoryCache());

        var result = await cache.GetAsync(LocationId, DataType.TempMax);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task PutThenGetAsync_RoundTripsEntry()
    {
        var cache = new AcornSatExtensionCache(new MemoryCache());
        var entry = CreateEntry(DataType.TempMax);

        await cache.PutAsync(entry);
        var result = await cache.GetAsync(LocationId, DataType.TempMax);

        Assert.IsNotNull(result);
        Assert.AreEqual(entry.ComparisonSignature, result!.ComparisonSignature);
        Assert.AreEqual(entry.Decision, result.Decision);
    }

    [TestMethod]
    public async Task PutThenGetAsync_DifferentDataTypeForSameLocation_DoesNotCollide()
    {
        var cache = new AcornSatExtensionCache(new MemoryCache());
        await cache.PutAsync(CreateEntry(DataType.TempMax));

        var result = await cache.GetAsync(LocationId, DataType.TempMin);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task PutThenGetAsync_DifferentLocationForSameDataType_DoesNotCollide()
    {
        var cache = new AcornSatExtensionCache(new MemoryCache());
        await cache.PutAsync(CreateEntry(DataType.TempMax));

        var result = await cache.GetAsync(Guid.NewGuid(), DataType.TempMax);

        Assert.IsNull(result);
    }

    private static AcornSatExtensionCacheEntry CreateEntry(DataType dataType)
    {
        return new AcornSatExtensionCacheEntry
        {
            LocationId = LocationId,
            DataType = dataType,
            AdjustedStationId = "023000",
            OpenCdoStationId = "023000",
            ComparisonYear = 2025,
            Decision = AcornSatExtensionDecision.Eligible,
            LatestAcornSatDate = new DateOnly(2025, 12, 31),
            ComparisonSignature = "signature",
            OverlayRecords = [],
        };
    }

    private sealed class MemoryCache : ICache
    {
        private readonly Dictionary<string, object?> values = [];

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
}
