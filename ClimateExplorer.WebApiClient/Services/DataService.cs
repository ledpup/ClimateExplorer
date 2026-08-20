namespace ClimateExplorer.WebApiClient.Services;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using static ClimateExplorer.Core.Enums;

public class DataService : IDataService
{
    /// <summary>
    /// Max number of entries retained by the response cache. Enforced by the injected IMemoryCache's
    /// SizeLimit, so this must match the SizeLimit configured where that IMemoryCache is created
    /// (see AddMemoryCache calls in Program.cs / ClimateExplorer.CachingTool).
    /// </summary>
    public const int CacheSizeLimit = 20;

    private readonly HttpClient httpClient;
    private readonly IMemoryCache memoryCache;
    private readonly JsonSerializerOptions jsonSerializerOptions;

    public DataService(
        HttpClient httpClient,
        IMemoryCache memoryCache)
    {
        this.httpClient = httpClient;
        this.memoryCache = memoryCache;
        jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    }

    private T? GetCached<T>(string key)
        where T : class
    {
        return memoryCache.TryGetValue(key, out var value) ? value as T : null;
    }

    private void SetCached<T>(string key, T val, TimeSpan? expiration = null)
        where T : class
    {
        var options = new MemoryCacheEntryOptions { Size = 1 };
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        memoryCache.Set(key, val, options);
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
        var response =
            await httpClient.PostAsJsonAsync(
                "dataset",
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
        var result = GetCached<DataSetDefinitionViewModel[]>(url);
        if (result == null)
        {
            result = await httpClient.GetFromJsonAsync<DataSetDefinitionViewModel[]>(url, jsonSerializerOptions);

            SetCached(url, result!);
        }
        
        return result!;
    }

    public async Task<IEnumerable<Location>?> GetLocations(bool permitCreateCache = true, bool fromCacheOnly = false)
    {
        var url = $"/location";
        
        if (!permitCreateCache)
        {
            url = QueryHelpers.AddQueryString(url, "permitCreateCache", permitCreateCache.ToString().ToLowerInvariant());
        }

        var result = GetCached<Location[]>(url);
        if (result == null)
        {
            if (fromCacheOnly)
            {
                return null;
            }

            result = await httpClient.GetFromJsonAsync<Location[]>(url);

            SetCached(url, result!);
        }

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

        var result = GetCached<LocationDistance[]>(url);
        if (result == null)
        {
            result = await httpClient.GetFromJsonAsync<LocationDistance[]>(url);

            SetCached(url, result!);
        }

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
        var result = GetCached<Region[]>(url);
        if (result == null)
        {
            result = await httpClient.GetFromJsonAsync<Region[]>(url);

            SetCached(url, result!);
        }
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

    public async Task<IReadOnlyList<DataSetMetadata>> GetLocationDataSetMetadata(Guid locationId)
    {
        var url = "/location-dataset-metadata";
        url = QueryHelpers.AddQueryString(url, "locationId", locationId.ToString());

        var result = GetCached<DataSetMetadata[]>(url);
        if (result == null)
        {
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Received non-success status code {response.StatusCode} with body {await response.Content.ReadAsStringAsync()}");
            }

            result = await response.Content.ReadFromJsonAsync<DataSetMetadata[]>(jsonSerializerOptions);

            SetCached(url, result!);
        }

        return result!;
    }

    public async Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable()
    {
        const string heatingScoreTableKey = "HeatingScoreTable";
        var result = GetCached<IEnumerable<HeatingScoreRow>>(heatingScoreTableKey);
        if (result == null)
        {
            var url = $"/heating-score-table";
            result = await httpClient.GetFromJsonAsync<HeatingScoreRow[]>(url);

            SetCached(heatingScoreTableKey, result!);
        }
        
        return result!;
    }

    public async Task<ClimateRecordsResponse?> GetClimateRecords(Guid locationId, DataType dataType = DataType.TempMax, DataAdjustment? dataAdjustment = null, bool ascending = false, int? take = null, int? skip = null, int? month = null, bool monthly = false, int? day = null, bool fromCacheOnly = false)
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

        var result = GetCached<ClimateRecordsResponse>(url);

        if (result == null)
        {
            if (fromCacheOnly)
            {
                return null;
            }

            result = await httpClient.GetFromJsonAsync<ClimateRecordsResponse>(url, jsonSerializerOptions);

            SetCached(url, result!, TimeSpan.FromHours(4));
        }

        return result!;
    }
}