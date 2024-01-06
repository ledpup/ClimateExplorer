using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Web.Services;

public class DataService : IDataService
{   
    private readonly HttpClient _httpClient;
    private readonly IDataServiceCache _dataServiceCache;
    JsonSerializerOptions _jsonSerializerOptions;

    public DataService(
        HttpClient httpClient,
        IDataServiceCache dataServiceCache)
    {
        _httpClient = httpClient;
        _dataServiceCache = dataServiceCache;
        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    }

    public async Task<DataSet> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment? dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId = null, short ? year = null, short? dayGrouping = 14, float? dayGroupingThreshold = .7f)
    {
        var url = $"dataSet/{dataType}/{resolution}";

        if (dataAdjustment != null)
        {
            url = QueryHelpers.AddQueryString(url, "dataAdjustment", dataAdjustment.Value.ToString());
        }
        if (locationId != null)
        {
            url = QueryHelpers.AddQueryString(url, "locationId", locationId.Value.ToString());
        }
        if (aggregationMethod != null)
        {
            url = QueryHelpers.AddQueryString(url, "aggregationMethod", aggregationMethod.Value.ToString());
        }
        if (year != null)
        {
            url = QueryHelpers.AddQueryString(url, "year", year.Value.ToString());
        }
        if (dayGrouping != null)
        {
            url = QueryHelpers.AddQueryString(url, "dayGrouping", dayGrouping.Value.ToString());
        }
        if (dayGroupingThreshold != null)
        {
            url = QueryHelpers.AddQueryString(url, "dayGroupingThreshold", dayGroupingThreshold.Value.ToString());
        }

        var result = _dataServiceCache.Get<DataSet>(url);

        if (result == null)
        {
            result = await _httpClient.GetFromJsonAsync<DataSet>(url, _jsonSerializerOptions);

            _dataServiceCache.Put(url, result!);
        }

        return result!;
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
        short? year)
    {
        var response = 
            await _httpClient.PostAsJsonAsync<PostDataSetsRequestBody>(
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
                    Anomaly = seriesValueOption == SeriesValueOptions.Anomaly,
                    FilterToYear = year,
                });

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Received non-success status code {response.StatusCode} with body {await response.Content.ReadAsStringAsync()}");
        }

        var result = await response.Content.ReadFromJsonAsync<DataSet>();

        return result!;
    }

    public async Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .5f, float regionThreshold = .5f)
    {
        var url = $"dataSet/{dataType}/{resolution}/{dataAdjustment}?dayGrouping={dayGrouping}&dayGroupingThreshold={dayGroupingThreshold}&regionThreshold={regionThreshold}";
        if (minLatitude != null)
        {
            url += $"&minLatitude={minLatitude}";
        }
        if (maxLatitude != null)
        {
            url += $"&maxLatitude={maxLatitude}";
        }
        var dataset = await _httpClient.GetFromJsonAsync<DataSet[]>(url);
        return dataset!;
    }

    public async Task<ApiMetadataModel> GetAbout()
    {
        var about = await _httpClient.GetFromJsonAsync<ApiMetadataModel>("/about");
        return about!;
    }

    public async Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions()
    {
        var url = "/datasetdefinition";
        var dataSetDefinitions = await _httpClient.GetFromJsonAsync<DataSetDefinitionViewModel[]>(url, _jsonSerializerOptions);
        return dataSetDefinitions!;
    }

    public async Task<IEnumerable<Location>> GetLocations(Guid? locationId = null)
    {
        var url = $"/location";
        if (locationId.HasValue && Guid.Empty != locationId)
        {
            url = QueryHelpers.AddQueryString(url, "locationId", locationId.Value.ToString());
        }

        var locations = await _httpClient.GetFromJsonAsync<Location[]>(url);
        return locations!;
    }

    public async Task<Dictionary<string, string>> GetCountries()
    {
        var url = $"/country";
        var countries = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>(url);
        return countries!;
    }

    public async Task<IEnumerable<Region>> GetRegions()
    {
        var url = $"/region";
        var result = await _httpClient.GetFromJsonAsync<Region[]>(url);
        return result!;
    }

    public async Task<Location> GetLocationByPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        var url = $"/location-by-path";
        url = QueryHelpers.AddQueryString(url, "path", path);
        
        var location = await _httpClient.GetFromJsonAsync<Location>(url);
        return location!;
    }
}