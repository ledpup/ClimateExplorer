namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public sealed class DataSetFreshnessPolicy(TimeProvider timeProvider)
{
    /// <summary>
    /// Downloader keys whose sources publish daily and back enough location traffic that a content-aware
    /// freshness check (see the <see cref="DataSetSourceState"/> overload) is worth the extra retry cost.
    /// Every other downloader keeps using the plain time-since-fetch window below.
    /// </summary>
    private static readonly HashSet<string> ContentAwareDownloaderKeys = ["bom-station", "ghcnd-station"];

    public bool IsFresh(ICachedData? cachedData, DataResolution dataResolution)
    {
        if (cachedData?.RetrievedDate == null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        if (cachedData.RetrievedDate.Value > now)
        {
            return false;
        }

        return cachedData.RetrievedDate.Value > now.Subtract(GetMaximumAge(dataResolution));
    }

    public bool IsFresh(DataSetSourceState? state, DataResolution dataResolution, string downloaderKey)
    {
        if (dataResolution != DataResolution.Daily || !ContentAwareDownloaderKeys.Contains(downloaderKey))
        {
            return IsFresh(state, dataResolution);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var hasYesterdaysRecord = state?.LatestRecordDate is { } latest && latest >= today.AddDays(-1) && latest <= today;

        return hasYesterdaysRecord || IsFreshWithinHours(state, contentAwareRetryWindowHours: 6);
    }

    private bool IsFreshWithinHours(ICachedData? cachedData, int contentAwareRetryWindowHours)
    {
        if (cachedData?.RetrievedDate == null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        if (cachedData.RetrievedDate.Value > now)
        {
            return false;
        }

        return cachedData.RetrievedDate.Value > now.Subtract(TimeSpan.FromHours(contentAwareRetryWindowHours));
    }

    private static TimeSpan GetMaximumAge(DataResolution dataResolution)
    {
        return dataResolution switch
        {
            DataResolution.Daily => TimeSpan.FromHours(24),
            DataResolution.Monthly => TimeSpan.FromDays(28),
            DataResolution.Yearly => TimeSpan.FromDays(28),
            _ => throw new NotSupportedException($"No automatic retrieval freshness interval is configured for {dataResolution}."),
        };
    }
}
