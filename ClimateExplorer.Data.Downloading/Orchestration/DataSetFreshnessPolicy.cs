namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public sealed class DataSetFreshnessPolicy(TimeProvider timeProvider)
{
    /// <summary>
    /// How long a content-aware source that doesn't yet hold yesterday's record is left before another request may
    /// check again. Kept short so a new day shows soon after the source publishes it - BOM's FTP daily tables add
    /// yesterday at about 06:30 UTC - rather than up to one window later.
    /// </summary>
    public static readonly TimeSpan ContentAwareRetryWindow = TimeSpan.FromMinutes(30);

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
        if (!CanBeCurrentByContent(dataResolution, downloaderKey))
        {
            return IsFresh(state, dataResolution);
        }

        return HasYesterdaysRecord(state?.LatestRecordDate) || IsFreshWithin(state, ContentAwareRetryWindow);
    }

    /// <summary>
    /// Whether a source can be judged current from its own content alone (see <see cref="HasYesterdaysRecord"/>),
    /// independent of when it was retrieved.
    /// </summary>
    public bool CanBeCurrentByContent(DataResolution dataResolution, string downloaderKey)
    {
        return dataResolution == DataResolution.Daily && ContentAwareDownloaderKeys.Contains(downloaderKey);
    }

    public bool HasYesterdaysRecord(DateOnly? latestRecordDate)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return latestRecordDate is { } latest && latest >= today.AddDays(-1) && latest <= today;
    }

    private bool IsFreshWithin(ICachedData? cachedData, TimeSpan window)
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

        return cachedData.RetrievedDate.Value > now.Subtract(window);
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
