#nullable enable
namespace ClimateExplorer.WebApi.AcornSat;

using System;
using System.Threading.Tasks;
using ClimateExplorer.Data.Downloading.Extenders;
using ClimateExplorer.WebApi.Infrastructure;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// A focused wrapper over <see cref="ClimateExplorerApiServices.LongtermCache"/> for
/// <see cref="AcornSatExtensionCacheEntry"/>, so callers (and their tests) key entries by location and data
/// type without depending on the underlying cache key/filename format.
/// </summary>
internal sealed class AcornSatExtensionCache(ICache cache)
{
    private readonly ICache cache = cache;

    public Task<AcornSatExtensionCacheEntry?> GetAsync(Guid locationId, DataType dataType)
    {
        return cache.Get<AcornSatExtensionCacheEntry?>(BuildKey(locationId, dataType));
    }

    public Task PutAsync(AcornSatExtensionCacheEntry entry)
    {
        return cache.Put(BuildKey(entry.LocationId, entry.DataType), entry);
    }

    private static string BuildKey(Guid locationId, DataType dataType)
    {
        return $"AcornSatExtension_v1_{locationId:D}_{dataType}";
    }
}
