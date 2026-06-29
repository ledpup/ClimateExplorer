#pragma warning disable SA1204
namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

public sealed class RecentObservationsDataProvider : IRecentObservationsDataProvider
{
    private readonly IDataService dataService;
    private readonly ILogger<RecentObservationsDataProvider>? logger;
    private readonly Dictionary<RecentObservationsDataCacheKey, Task<RecentObservationsDataSet>> cache = [];

    // The recent-observations endpoint returns all series (max, min, precipitation)
    // for a location in one call, so share that fetch across both tabs rather than
    // requesting per data type. This avoids re-downloading the GHCNd station CSV
    // (and re-fetching the BOM obs files) once per metric.
    private readonly Dictionary<Guid, Task<RecentObservationsResponse>> recentResponseCache = [];

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
        var recentTask = GetRecentObservations(locationId);
        var historicalMaxTask = GetHistoricalRecords(locationId, DataType.TempMax, DataAdjustment.Unadjusted);
        var historicalMinTask = GetHistoricalRecords(locationId, DataType.TempMin, DataAdjustment.Unadjusted);

        await Task.WhenAll(recentTask, historicalMaxTask, historicalMinTask);

        var recentResponse = await recentTask;
        var historicalMaxResponse = await historicalMaxTask;
        var historicalMinResponse = await historicalMinTask;

        var recentMaxRecords = recentResponse.TempMax?.Records ?? [];
        var recentMinRecords = recentResponse.TempMin?.Records ?? [];

        var hasHistoricalMaxMin = historicalMaxResponse.Records.Count > 0 && historicalMinResponse.Records.Count > 0;
        var meanRecords = hasHistoricalMaxMin
            ? new List<DataRecord>()
            : (await GetHistoricalRecords(locationId, DataType.TempMean, DataAdjustment.Unadjusted)).Records;

        if (!recentResponse.IsSupported &&
            !hasHistoricalMaxMin &&
            meanRecords.Count == 0)
        {
            return RecentObservationsDataSet.UnsupportedTemperature();
        }

        return RecentObservationsDataSet.Temperature(
            MergeDailyDataRecords(historicalMaxResponse.Records, recentMaxRecords),
            MergeDailyDataRecords(historicalMinResponse.Records, recentMinRecords),
            MergeDailyDataRecords(meanRecords, Array.Empty<DataRecord>()),
            hasHistoricalMaxMin,
            CreateSourceMetadata(recentResponse.TempMax?.SourceMetadata, recentResponse.TempMin?.SourceMetadata));
    }

    private async Task<RecentObservationsDataSet> FetchPrecipitationData(Guid locationId)
    {
        var recentTask = GetRecentObservations(locationId);
        var historicalTask = GetHistoricalRecords(locationId, DataType.Precipitation, null);
        await Task.WhenAll(recentTask, historicalTask);

        var recentResponse = await recentTask;
        var historicalResponse = await historicalTask;

        var recentPrecipitation = recentResponse.Precipitation;

        if (recentPrecipitation is null && historicalResponse.Records.Count == 0)
        {
            return RecentObservationsDataSet.UnsupportedPrecipitation();
        }

        return RecentObservationsDataSet.Precipitation(
            MergeDailyDataRecords(historicalResponse.Records, recentPrecipitation?.Records ?? []),
            CreateSourceMetadata(recentPrecipitation?.SourceMetadata));
    }

    private Task<RecentObservationsResponse> GetRecentObservations(Guid locationId)
    {
        if (recentResponseCache.TryGetValue(locationId, out var cached))
        {
            logger?.LogDebug("Using cached recent observations response for location {LocationId}", locationId);
            return cached;
        }

        var task = dataService.GetRecentObservations(locationId);
        recentResponseCache[locationId] = task;
        return AwaitAndEvictOnFailure(locationId, task);
    }

    private async Task<RecentObservationsResponse> AwaitAndEvictOnFailure(Guid locationId, Task<RecentObservationsResponse> task)
    {
        try
        {
            return await task;
        }
        catch
        {
            if (recentResponseCache.TryGetValue(locationId, out var cachedTask) && ReferenceEquals(cachedTask, task))
            {
                recentResponseCache.Remove(locationId);
            }

            throw;
        }
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
