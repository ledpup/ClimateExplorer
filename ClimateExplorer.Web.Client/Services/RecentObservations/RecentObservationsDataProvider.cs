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
        var historicalMaxTask = GetRecords(locationId, DataType.TempMax, DataAdjustment.Unadjusted);
        var historicalMinTask = GetRecords(locationId, DataType.TempMin, DataAdjustment.Unadjusted);

        await Task.WhenAll(historicalMaxTask, historicalMinTask);

        var historicalMaxResponse = await historicalMaxTask;
        var historicalMinResponse = await historicalMinTask;

        if (!historicalMaxResponse.DataResolution.HasValue && !historicalMinResponse.DataResolution.HasValue)
        {
            return RecentObservationsDataSet.UnsupportedTemperature();
        }

        var hasHistoricalMaxMin = historicalMaxResponse.Records.Count > 0 && historicalMinResponse.Records.Count > 0;
        var meanRecords = hasHistoricalMaxMin
            ? new List<DataRecord>()
            : (await GetRecords(locationId, DataType.TempMean, DataAdjustment.Unadjusted)).Records;

        return RecentObservationsDataSet.Temperature(
            historicalMaxResponse.Records,
            historicalMinResponse.Records,
            meanRecords,
            hasHistoricalMaxMin,
            CreateSourceMetadata(historicalMaxResponse, historicalMinResponse));
    }

    private async Task<RecentObservationsDataSet> FetchPrecipitationData(Guid locationId)
    {
        var historicalResponse = await GetRecords(locationId, DataType.Precipitation, null);

        if (!historicalResponse.DataResolution.HasValue)
        {
            return RecentObservationsDataSet.UnsupportedPrecipitation();
        }

        return RecentObservationsDataSet.Precipitation(
            historicalResponse.Records,
            CreateSourceMetadata(historicalResponse));
    }

    private async Task<ClimateRecordsResponse> GetRecords(
        Guid locationId,
        DataType dataType,
        DataAdjustment? preferredAdjustment)
    {
        ClimateRecordsResponse response = new() { DataType = dataType, DataAdjustment = preferredAdjustment };

        foreach (var adjustment in GetAdjustmentCandidates(dataType, preferredAdjustment))
        {
            response = (await dataService.GetClimateRecords(locationId, dataType, adjustment, monthly: false))!;
            if (response.Records.Count > 0)
            {
                return response;
            }
        }

        return response;
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

    private static IReadOnlyList<RecentObservationSourceMetadata> CreateSourceMetadata(params ClimateRecordsResponse[] responses)
    {
        return [.. responses.SelectMany(MapSourceMetadata)];
    }

    private static IEnumerable<RecentObservationSourceMetadata> MapSourceMetadata(ClimateRecordsResponse response)
    {
        if (response.SourceMetadata is null)
        {
            yield break;
        }

        foreach (var dataSetMetadata in response.SourceMetadata)
        {
            yield return new RecentObservationSourceMetadata
            {
                SourceCode = dataSetMetadata.SourceCode,
                SourceName = dataSetMetadata.SourceName,
                StationId = dataSetMetadata.Stations.SingleOrDefault(x => x.StationEndDate is null)?.StationId,
                SourceUrl = dataSetMetadata.SourceUrl,
                SourceUrlLabel = dataSetMetadata.SourceUrlLabel,
                RetrievedAtUtc = response.RetrievedDate,
            };
        }
    }

    private readonly record struct RecentObservationsDataCacheKey(Guid LocationId, RecentObservationsTab Tab);
}
