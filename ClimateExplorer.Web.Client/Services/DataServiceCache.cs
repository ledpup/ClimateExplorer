namespace ClimateExplorer.Web.Services;

public interface IDataServiceCache
{
    T? Get<T>(string key) where T : class;
    void Put<T>(string key, T val) where T : class;
}

public class DataServiceCache : IDataServiceCache
{
    private class CacheEntry
    {
        public DateTime LastHitUtc { get; set; }
        public object? Value { get; set; }
    }

    readonly Dictionary<string, CacheEntry> entries = new Dictionary<string, CacheEntry>();
    readonly ILogger<DataServiceCache> _logger;
    const int MaxEntryCount = 20;

    public DataServiceCache(ILogger<DataServiceCache> logger)
    {
        _logger = logger;
    }

    public T? Get<T>(string key) where T : class
    {
        lock (entries)
        {
            if (entries.TryGetValue(key, out CacheEntry? entry))
            {
                if (entry != null)
                {
                    _logger.LogInformation($"DataServiceCache.Get({key}) - hit");

                    entry.LastHitUtc = DateTime.UtcNow;

                    return entry.Value as T;
                }
            }

            _logger.LogInformation($"DataServiceCache.Get({key}) - miss");

            return null;
        }
    }

    void Evict()
    {
        // Remove LRU entries beyond max number of entries
        if (entries.Count > MaxEntryCount)
        {
            _logger.LogInformation($"$DataServiceCache.Evict() - triggering eviction as entry count is {entries.Count}, which is > {MaxEntryCount}");

            var keysToRemove =
                entries
                .OrderBy(x => x.Value.LastHitUtc)
                .Take(entries.Count - MaxEntryCount)
                .Select(x => x.Key)
                .ToArray();

            foreach (var keyToRemove in keysToRemove)
            {
                _logger.LogInformation($"$DataServiceCache.Evict() - evicting entry ${keyToRemove}");
                entries.Remove(keyToRemove);
            }

            _logger.LogInformation($"$DataServiceCache.Evict() - eviction complete, entry count is now {entries.Count}");
        }
    }

    public void Put<T>(string key, T val) where T : class
    {
        lock (entries)
        {
            _logger.LogInformation($"DataServiceCache.Put({key})");

            entries[key] =
                new CacheEntry()
                {
                    LastHitUtc = DateTime.UtcNow,
                    Value = val
                };

            Evict();
        }
    }
}
