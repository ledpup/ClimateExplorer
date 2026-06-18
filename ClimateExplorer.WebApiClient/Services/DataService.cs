namespace ClimateExplorer.WebApiClient.Services;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

public class DataService : IDataService
{
    private readonly HttpClient httpClient;
    private readonly IDataServiceCache dataServiceCache;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ILogger<DataService> logger;

    public DataService(
        HttpClient httpClient,
        IDataServiceCache dataServiceCache,
        ILogger<DataService> logger)
    {
        this.httpClient = httpClient;
        this.dataServiceCache = dataServiceCache;
        this.logger = logger;
        jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    }

    public async Task<DataSet> PostDataSet(
        BinGranularities binGranularity,
        ContainerAggregationFunctions binAggregationFunction,
        ContainerAggregationFunctions bucketAggregationFunction,
        ContainerAggregationFunctions cupAggregationFunction,
        SeriesValueOptions seriesValueOption,
        SeriesSpecification[] seriesSpecifications,
        SeriesDerivationTypes seriesDerivationType,
        float requiredBinDataProportion,
        float requiredBucketDataProportion,
        float requiredCupDataProportion,
        int cupSize,
        SeriesTransformations seriesTransformation,
        string? customTransformation,
        short? year,
        DataResolution? minimumDataResolution)
    {
        var endpoint = "dataset";
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var response =
            await httpClient.PostAsJsonAsync(
                endpoint,
                new PostDataSetsRequestBody
                {
                    BinAggregationFunction = binAggregationFunction,
                    BucketAggregationFunction = bucketAggregationFunction,
                    CupAggregationFunction = cupAggregationFunction,
                    BinningRule = binGranularity,
                    CupSize = cupSize,
                    RequiredBinDataProportion = requiredBinDataProportion,
                    RequiredBucketDataProportion = requiredBucketDataProportion,
                    RequiredCupDataProportion = requiredCupDataProportion,
                    SeriesDerivationType = seriesDerivationType,
                    SeriesSpecifications = seriesSpecifications,
                    SeriesTransformation = seriesTransformation,
                    CustomTransformation = customTransformation,
                    Anomaly = seriesValueOption == SeriesValueOptions.Anomaly,
                    FilterToYear = year,
                    MinimumDataResolution = minimumDataResolution,
                });

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Received non-success status code {response.StatusCode} with body {await response.Content.ReadAsStringAsync()}");
        }

        var result = await response.Content.ReadFromJsonAsync<DataSet>();
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        logger.LogInformation("PerfApiClient endpoint={Endpoint} method=POST cached=false elapsedMs={ElapsedMilliseconds:0.0} recordCount={RecordCount} seriesCount={SeriesCount} binGranularity={BinGranularity}", endpoint, elapsed, result?.DataRecords?.Count, seriesSpecifications.Length, binGranularity);

        return result!;
    }

    public async Task<ApiMetadataModel> GetAbout()
    {
        var about = await httpClient.GetFromJsonAsync<ApiMetadataModel>("/about");
        return about!;
    }

    public async Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions()
    {
        var url = "/datasetdefinition";
        var result = await GetFromJsonWithTiming<DataSetDefinitionViewModel[]>(url, jsonSerializerOptions, cacheable: true);
        
        return result!;
    }

    public async Task<IEnumerable<Location>> GetLocations(bool permitCreateCache = true)
    {
        var url = $"/location";
        
        if (!permitCreateCache)
        {
            url = QueryHelpers.AddQueryString(url, "permitCreateCache", permitCreateCache.ToString().ToLowerInvariant());
        }

        var result = await GetFromJsonWithTiming<Location[]>(url, cacheable: true);

        return result!;
    }

    public async Task<IEnumerable<LocationDistance>> GetNearbyLocations(Guid locationId, int? take = null, int? skip = null)
    {
        var url = $"/nearby-locations";
        url = QueryHelpers.AddQueryString(url, "locationId", locationId.ToString());

        if (take.HasValue)
        {
            url = QueryHelpers.AddQueryString(url, "take", take.Value.ToString());
        }

        if (skip.HasValue)
        {
            url = QueryHelpers.AddQueryString(url, "skip", skip.Value.ToString());
        }

        var result = await GetFromJsonWithTiming<LocationDistance[]>(url, cacheable: true);

        return result!;
    }

    public async Task<Dictionary<string, string>> GetCountries()
    {
        var url = $"/country";
        var countries = await httpClient.GetFromJsonAsync<Dictionary<string, string>>(url);
        return countries!;
    }

    public async Task<IEnumerable<Region>> GetRegions()
    {
        var url = $"/region";
        var result = await GetFromJsonWithTiming<Region[]>(url, cacheable: true);
        return result!;
    }

    public async Task<Location> GetLocationByPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        var url = $"/location-by-path";
        url = QueryHelpers.AddQueryString(url, "path", path);

        var location = await httpClient.GetFromJsonAsync<Location>(url);
        return location!;
    }

    public async Task<Location?> GetLocationById(Guid locationId)
    {
        var url = $"/location-by-id";
        url = QueryHelpers.AddQueryString(url, "locationId", locationId.ToString());

        var location = await httpClient.GetFromJsonAsync<Location?>(url);
        return location;
    }

    public async Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable()
    {
        const string heatingScoreTableKey = "HeatingScoreTable";
        var result = dataServiceCache.Get<IEnumerable<HeatingScoreRow>>(heatingScoreTableKey);
        if (result == null)
        {
            var url = $"/heating-score-table";
            result = await GetFromJsonWithTiming<HeatingScoreRow[]>(url, cacheable: true);
        }
        
        return result!;
    }

    public async Task<ClimateRecordsResponse> GetClimateRecords(Guid locationId, DataType dataType = DataType.TempMax, DataAdjustment? dataAdjustment = null, bool ascending = false, int? take = null, int? skip = null, int? month = null, bool monthly = false, int? day = null)
    {
        var url = "/climate-record";
        url = QueryHelpers.AddQueryString(url, "locationId", locationId.ToString());
        url = QueryHelpers.AddQueryString(url, "dataType", dataType.ToString());
        if (dataAdjustment.HasValue)
        {
            url = QueryHelpers.AddQueryString(url, "dataAdjustment", dataAdjustment.Value.ToString());
        }

        url = QueryHelpers.AddQueryString(url, "ascending", ascending.ToString().ToLowerInvariant());

        if (take.HasValue)
        {
            url = QueryHelpers.AddQueryString(url, "take", take.Value.ToString());
        }

        if (skip.HasValue)
        {
            url = QueryHelpers.AddQueryString(url, "skip", skip.Value.ToString());
        }

        if (month.HasValue)
        {
            url = QueryHelpers.AddQueryString(url, "month", month.Value.ToString());
        }

        if (day.HasValue)
        {
            url = QueryHelpers.AddQueryString(url, "day", day.Value.ToString());
        }

        if (monthly)
        {
            url = QueryHelpers.AddQueryString(url, "monthly", "true");
        }

        var result = await GetFromJsonWithTiming<ClimateRecordsResponse>(url, jsonSerializerOptions, cacheable: true);

        return result!;
    }

    public async Task<RecentObservationsResponse> GetRecentObservations(Guid locationId, DataType dataType, bool isLocationSupported = false)
    {
        var url = "/recent-observations";
        url = QueryHelpers.AddQueryString(url, "locationId", locationId.ToString());
        url = QueryHelpers.AddQueryString(url, "dataType", dataType.ToString());

        if (isLocationSupported)
        {
            url = QueryHelpers.AddQueryString(url, "isLocationSupported", "true");
        }

        var result = await GetFromJsonWithTiming<RecentObservationsResponse>(url, jsonSerializerOptions);
        return result!;
    }


    private async Task<T> GetFromJsonWithTiming<T>(string url, JsonSerializerOptions? options = null, bool cacheable = false)
    {
        var cached = cacheable ? dataServiceCache.Get<T>(url) : default;
        if (cached is not null)
        {
            logger.LogInformation("PerfApiClient endpoint={Endpoint} cached=true elapsedMs={ElapsedMilliseconds:0.0} recordCount={RecordCount}", url, 0, GetRecordCount(cached));
            return cached;
        }

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var result = await httpClient.GetFromJsonAsync<T>(url, options);
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        logger.LogInformation("PerfApiClient endpoint={Endpoint} cached=false elapsedMs={ElapsedMilliseconds:0.0} recordCount={RecordCount} responseType={ResponseType}", url, elapsed, GetRecordCount(result), typeof(T).Name);

        if (cacheable)
        {
            dataServiceCache.Put(url, result!);
        }

        return result!;
    }

    private static int? GetRecordCount<T>(T? result)
    {
        return result switch
        {
            System.Collections.ICollection collection => collection.Count,
            ClimateRecordsResponse response => response.Records?.Count(),
            RecentObservationsResponse response => response.Records?.Count(),
            _ => null,
        };
    }

}
