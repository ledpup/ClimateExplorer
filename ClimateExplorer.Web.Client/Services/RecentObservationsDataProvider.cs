#pragma warning disable SA1204
namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

public sealed class RecentObservationsDataProvider : IRecentObservationsDataProvider
{
    private readonly IDataService dataService;
    private readonly ILogger<RecentObservationsDataProvider>? logger;
    private readonly Dictionary<RecentObservationsDataCacheKey, Task<RecentObservationsDataSet>> cache = [];

    public RecentObservationsDataProvider(
        IDataService dataService,
        ILogger<RecentObservationsDataProvider>? logger = null)
    {
        this.dataService = dataService;
        this.logger = logger;
    }

    public Task<RecentObservationsDataSet> LoadTemperatureData(Location location)
    {
        return GetOrCreate(
            new RecentObservationsDataCacheKey(location.Id, RecentObservationsTab.Temperature),
            () => FetchTemperatureData(location.Id));
    }

    public Task<RecentObservationsDataSet> LoadPrecipitationData(Location location)
    {
        return GetOrCreate(
            new RecentObservationsDataCacheKey(location.Id, RecentObservationsTab.Precipitation),
            () => FetchPrecipitationData(location.Id));
    }

    private async Task<RecentObservationsDataSet> GetOrCreate(
        RecentObservationsDataCacheKey key,
        Func<Task<RecentObservationsDataSet>> fetch)
    {
        if (cache.TryGetValue(key, out var cached))
        {
            logger?.LogDebug("Using cached recent observations {Tab} data for location {LocationId}", key.Tab, key.LocationId);
            return await cached;
        }

        logger?.LogInformation("Loading recent observations {Tab} data for location {LocationId}", key.Tab, key.LocationId);
        var task = fetch();
        cache[key] = task;

        try
        {
            return await task;
        }
        catch
        {
            if (cache.TryGetValue(key, out var cachedTask) && ReferenceEquals(cachedTask, task))
            {
                cache.Remove(key);
            }

            throw;
        }
    }

    private async Task<RecentObservationsDataSet> FetchTemperatureData(Guid locationId)
    {
        var recentMaxTask = dataService.GetRecentObservations(locationId, DataType.TempMax);
        var recentMinTask = dataService.GetRecentObservations(locationId, DataType.TempMin);
        var historicalMaxTask = GetHistoricalRecords(locationId, DataType.TempMax, DataAdjustment.Unadjusted);
        var historicalMinTask = GetHistoricalRecords(locationId, DataType.TempMin, DataAdjustment.Unadjusted);

        await Task.WhenAll(recentMaxTask, recentMinTask, historicalMaxTask, historicalMinTask);

        var recentMaxResponse = await recentMaxTask;
        var recentMinResponse = await recentMinTask;
        var historicalMaxResponse = await historicalMaxTask;
        var historicalMinResponse = await historicalMinTask;

        var hasHistoricalMaxMin = historicalMaxResponse.Records.Count > 0 && historicalMinResponse.Records.Count > 0;
        var meanRecords = hasHistoricalMaxMin
            ? new List<DataRecord>()
            : (await GetHistoricalRecords(locationId, DataType.TempMean, DataAdjustment.Unadjusted)).Records;

        if ((!recentMaxResponse.IsSupported || !recentMinResponse.IsSupported) &&
            !hasHistoricalMaxMin &&
            meanRecords.Count == 0)
        {
            return RecentObservationsDataSet.UnsupportedTemperature();
        }

        return RecentObservationsDataSet.Temperature(
            MergeDailyDataRecords(historicalMaxResponse.Records, recentMaxResponse.Records),
            MergeDailyDataRecords(historicalMinResponse.Records, recentMinResponse.Records),
            MergeDailyDataRecords(meanRecords, Array.Empty<DataRecord>()),
            hasHistoricalMaxMin,
            CreateSourceMetadata(recentMaxResponse.SourceMetadata, recentMinResponse.SourceMetadata));
    }

    private async Task<RecentObservationsDataSet> FetchPrecipitationData(Guid locationId)
    {
        var recentTask = dataService.GetRecentObservations(locationId, DataType.Precipitation);
        var historicalTask = GetHistoricalRecords(locationId, DataType.Precipitation, null);
        await Task.WhenAll(recentTask, historicalTask);

        var recentResponse = await recentTask;
        var historicalResponse = await historicalTask;

        if (!recentResponse.IsSupported && historicalResponse.Records.Count == 0)
        {
            return RecentObservationsDataSet.UnsupportedPrecipitation();
        }

        return RecentObservationsDataSet.Precipitation(
            MergeDailyDataRecords(historicalResponse.Records, recentResponse.Records),
            CreateSourceMetadata(recentResponse.SourceMetadata));
    }

    private async Task<ClimateRecordsResponse> GetHistoricalRecords(
        Guid locationId,
        DataType dataType,
        DataAdjustment? preferredAdjustment)
    {
        foreach (var adjustment in GetAdjustmentCandidates(dataType, preferredAdjustment))
        {
            var response = await dataService.GetClimateRecords(locationId, dataType, adjustment, monthly: false);
            if (response.Records.Count > 0)
            {
                return response;
            }
        }

        return new ClimateRecordsResponse
        {
            DataType = dataType,
            DataAdjustment = preferredAdjustment,
            DataResolution = DataResolution.Daily,
        };
    }

    private static List<DataRecord> MergeDailyDataRecords(
        IEnumerable<DataRecord> historicalRecords,
        IEnumerable<DataRecord> recentRecords)
    {
        var recordsByDate = new SortedDictionary<DateOnly, DataRecord>();

        foreach (var record in historicalRecords.Where(x => x.Date.HasValue && x.Value.HasValue))
        {
            recordsByDate[record.Date!.Value] = record;
        }

        foreach (var record in recentRecords.Where(x => x.Date.HasValue && x.Value.HasValue))
        {
            recordsByDate[record.Date!.Value] = record;
        }

        return [.. recordsByDate.Values];
    }

    private static IEnumerable<DataAdjustment?> GetAdjustmentCandidates(DataType dataType, DataAdjustment? preferredAdjustment)
    {
        if (dataType == DataType.Precipitation)
        {
            yield return null;
            yield break;
        }

        if (preferredAdjustment.HasValue)
        {
            yield return preferredAdjustment.Value;
        }

        yield return DataAdjustment.Unadjusted;
    }

    private static IReadOnlyList<RecentObservationSourceMetadata> CreateSourceMetadata(params RecentObservationSourceMetadata?[] sourceMetadata)
    {
        return [.. sourceMetadata.Where(x => x is not null).Select(x => x!)];
    }

    private readonly record struct RecentObservationsDataCacheKey(Guid LocationId, RecentObservationsTab Tab);
}
