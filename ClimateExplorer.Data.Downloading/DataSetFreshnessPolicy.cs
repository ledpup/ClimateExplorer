namespace ClimateExplorer.Data.Downloading;

using ClimateExplorer.Core.Interface;
using static ClimateExplorer.Core.Enums;

public sealed class DataSetFreshnessPolicy(TimeProvider timeProvider)
{
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

        var maximumAge = dataResolution switch
        {
            DataResolution.Daily => TimeSpan.FromHours(24),
            DataResolution.Monthly => TimeSpan.FromDays(28),
            _ => throw new NotSupportedException($"No automatic retrieval freshness interval is configured for {dataResolution}."),
        };

        return cachedData.RetrievedDate.Value > now.Subtract(maximumAge);
    }
}
