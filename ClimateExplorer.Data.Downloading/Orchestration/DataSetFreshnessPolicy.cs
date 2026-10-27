namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public sealed class DataSetFreshnessPolicy(TimeProvider timeProvider)
{
    /// <summary>
    /// How long a BOM station that doesn't yet hold yesterday's record is left before another request may check
    /// again. Kept short so a new day shows soon after BOM publishes it - its FTP daily tables add yesterday at
    /// about 06:30 UTC - and each check is only a small monthly table.
    /// </summary>
    public static readonly TimeSpan BomRetryWindow = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The same for GHCNd stations. Longer, because each check downloads the station's whole history from NOAA,
    /// and GHCNd often runs several days behind, so a short window would repeat that download all day.
    /// </summary>
    public static readonly TimeSpan GhcndRetryWindow = TimeSpan.FromHours(6);

    /// <summary>
    /// Downloader keys whose sources publish daily and back enough location traffic that a content-aware
    /// freshness check (see the <see cref="DataSetSourceState"/> overload) is worth the extra retry cost, with how
    /// long to wait between checks while yesterday's record is missing. Every other downloader keeps using the
    /// plain time-since-fetch window below.
    /// </summary>
    private static readonly Dictionary<string, TimeSpan> ContentAwareRetryWindows = new(StringComparer.Ordinal)
    {
        ["bom-station"] = BomRetryWindow,
        ["ghcnd-station"] = GhcndRetryWindow,
    };

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

        return HasYesterdaysRecord(state?.LatestRecordDate) || IsFreshWithin(state, ContentAwareRetryWindows[downloaderKey]);
    }

    /// <summary>
    /// Whether a source can be judged current from its own content alone (see <see cref="HasYesterdaysRecord"/>),
    /// independent of when it was retrieved.
    /// </summary>
    public bool CanBeCurrentByContent(DataResolution dataResolution, string downloaderKey)
    {
        return dataResolution == DataResolution.Daily && ContentAwareRetryWindows.ContainsKey(downloaderKey);
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
