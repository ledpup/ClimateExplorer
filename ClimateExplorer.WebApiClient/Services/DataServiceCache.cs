using Microsoft.Extensions.Logging;

namespace ClimateExplorer.WebApiClient.Services;

public interface IDataServiceCache
{
    T? Get<T>(string key)
        where T : class;
    void Put<T>(string key, T val)
        where T : class;
}

public class DataServiceCache : IDataServiceCache
{
    private const int MaxEntryCount = 20;
    private readonly Dictionary<string, CacheEntry> entries = [];
    private readonly ILogger<DataServiceCache> logger;

    public DataServiceCache(ILogger<DataServiceCache> logger)
    {
        this.logger = logger;
    }

    public T? Get<T>(string key)
        where T : class
    {
        lock (entries)
        {
            if (entries.TryGetValue(key, out CacheEntry? entry))
            {
                if (entry != null)
                {
                    logger.LogInformation($"DataServiceCache.Get({key}) - hit");

                    entry.LastHitUtc = DateTime.UtcNow;

                    return entry.Value as T;
                }
            }

            logger.LogInformation($"DataServiceCache.Get({key}) - miss");

            return null;
        }
    }

    public void Put<T>(string key, T val)
    where T : class
    {
        lock (entries)
        {
            logger.LogInformation($"DataServiceCache.Put({key})");

            entries[key] =
                new CacheEntry()
                {
                    LastHitUtc = DateTime.UtcNow,
                    Value = val,
                };

            Evict();
        }
    }

    private void Evict()
    {
        // Remove LRU entries beyond max number of entries
        if (entries.Count > MaxEntryCount)
        {
            logger.LogInformation($"$DataServiceCache.Evict() - triggering eviction as entry count is {entries.Count}, which is > {MaxEntryCount}");

            var keysToRemove =
                entries
                .OrderBy(x => x.Value.LastHitUtc)
                .Take(entries.Count - MaxEntryCount)
                .Select(x => x.Key)
                .ToArray();

            foreach (var keyToRemove in keysToRemove)
            {
                logger.LogInformation($"$DataServiceCache.Evict() - evicting entry ${keyToRemove}");
                entries.Remove(keyToRemove);
            }

            logger.LogInformation($"$DataServiceCache.Evict() - eviction complete, entry count is now {entries.Count}");
        }
    }

    private class CacheEntry
    {
        public DateTime LastHitUtc { get; set; }
        public object? Value { get; set; }
    }
}
