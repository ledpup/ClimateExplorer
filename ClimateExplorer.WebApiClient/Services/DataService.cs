namespace ClimateExplorer.WebApiClient.Services;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using Microsoft.AspNetCore.WebUtilities;
using static ClimateExplorer.Core.Enums;

public class DataService : IDataService
{
    private readonly HttpClient httpClient;
    private readonly IDataServiceCache dataServiceCache;
    private readonly JsonSerializerOptions jsonSerializerOptions;    

    public DataService(
        HttpClient httpClient,
        IDataServiceCache dataServiceCache)
    {
        this.httpClient = httpClient;
        this.dataServiceCache = dataServiceCache;
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
        var result = dataServiceCache.Get<DataSetDefinitionViewModel[]>(url);
        if (result == null)
        {
            result = await httpClient.GetFromJsonAsync<DataSetDefinitionViewModel[]>(url, jsonSerializerOptions);

            dataServiceCache.Put(url, result!);
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

        var result = dataServiceCache.Get<Location[]>(url);
        if (result == null)
        {
            if (fromCacheOnly)
            {
                return null;
            }

            result = await httpClient.GetFromJsonAsync<Location[]>(url);

            dataServiceCache.Put(url, result!);
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

        var result = dataServiceCache.Get<LocationDistance[]>(url);
        if (result == null)
        {
            result = await httpClient.GetFromJsonAsync<LocationDistance[]>(url);

            dataServiceCache.Put(url, result!);
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
        var result = dataServiceCache.Get<Region[]>(url);
        if (result == null)
        {
            result = await httpClient.GetFromJsonAsync<Region[]>(url);

            dataServiceCache.Put(url, result!);
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

        var result = dataServiceCache.Get<DataSetMetadata[]>(url);
        if (result == null)
        {
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Received non-success status code {response.StatusCode} with body {await response.Content.ReadAsStringAsync()}");
            }

            result = await response.Content.ReadFromJsonAsync<DataSetMetadata[]>(jsonSerializerOptions);

            dataServiceCache.Put(url, result!);
        }

        return result!;
    }

    public async Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable()
    {
        const string heatingScoreTableKey = "HeatingScoreTable";
        var result = dataServiceCache.Get<IEnumerable<HeatingScoreRow>>(heatingScoreTableKey);
        if (result == null)
        {
            var url = $"/heating-score-table";
            result = await httpClient.GetFromJsonAsync<HeatingScoreRow[]>(url);

            dataServiceCache.Put(heatingScoreTableKey, result!);
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

        var result = dataServiceCache.Get<ClimateRecordsResponse>(url);

        if (result == null)
        {
            if (fromCacheOnly)
            {
                return null;
            }

            result = await httpClient.GetFromJsonAsync<ClimateRecordsResponse>(url, jsonSerializerOptions);

            dataServiceCache.Put(url, result!);
        }

        return result!;
    }

    public async Task<RecentObservationsResponse> GetRecentObservations(Guid locationId)
    {
        var url = "/recent-observations";
        url = QueryHelpers.AddQueryString(url, "locationId", locationId.ToString());

        var result = await httpClient.GetFromJsonAsync<RecentObservationsResponse>(url, jsonSerializerOptions);
        return result!;
    }
}
