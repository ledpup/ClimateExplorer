namespace ClimateExplorer.Web.Services;

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

    public async Task<DataSet> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment? dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId = null, short? year = null, short? groupingDays = 14, float? groupingThreshold = .7f)
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

        if (groupingDays != null)
        {
            url = QueryHelpers.AddQueryString(url, "groupingDays", groupingDays.Value.ToString());
        }

        if (groupingThreshold != null)
        {
            url = QueryHelpers.AddQueryString(url, "groupingThreshold", groupingThreshold.Value.ToString());
        }

        var result = dataServiceCache.Get<DataSet>(url);

        if (result == null)
        {
            result = await httpClient.GetFromJsonAsync<DataSet>(url, jsonSerializerOptions);

            dataServiceCache.Put(url, result!);
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
        short? year,
        DataResolution? minimumDataResolution)
    {
        var response =
            await httpClient.PostAsJsonAsync<PostDataSetsRequestBody>(
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
                    MinimumDataResolution = minimumDataResolution,
                });

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Received non-success status code {response.StatusCode} with body {await response.Content.ReadAsStringAsync()}");
        }

        var result = await response.Content.ReadFromJsonAsync<DataSet>();

        return result!;
    }

    public async Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short groupingDays = 14, float groupingThreshold = .5f, float regionThreshold = .5f)
    {
        var url = $"dataSet/{dataType}/{resolution}/{dataAdjustment}?groupingDays={groupingDays}&groupingThreshold={groupingThreshold}&regionThreshold={regionThreshold}";
        if (minLatitude != null)
        {
            url += $"&minLatitude={minLatitude}";
        }

        if (maxLatitude != null)
        {
            url += $"&maxLatitude={maxLatitude}";
        }

        var dataset = await httpClient.GetFromJsonAsync<DataSet[]>(url);
        return dataset!;
    }

    public async Task<ApiMetadataModel> GetAbout()
    {
        var about = await httpClient.GetFromJsonAsync<ApiMetadataModel>("/about");
        return about!;
    }

    public async Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions()
    {
        var url = "/datasetdefinition";
        var dataSetDefinitions = await httpClient.GetFromJsonAsync<DataSetDefinitionViewModel[]>(url, jsonSerializerOptions);
        return dataSetDefinitions!;
    }

    public async Task<IEnumerable<Location>> GetLocations(Guid? locationId = null)
    {
        var url = $"/location";
        if (locationId.HasValue && locationId != Guid.Empty)
        {
            url = QueryHelpers.AddQueryString(url, "locationId", locationId.Value.ToString());
        }

        var locations = await httpClient.GetFromJsonAsync<Location[]>(url);
        return locations!;
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
        var result = await httpClient.GetFromJsonAsync<Region[]>(url);
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

    public async Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable()
    {
        var url = $"/heating-score-table";
        var result = await httpClient.GetFromJsonAsync<HeatingScoreRow[]>(url);
        return result!;
    }

    public async Task<IEnumerable<ClimateRecord>> GetClimateRecords(Guid locationId)
    {
        var url = $"/climate-record?locationId={locationId}";
        var result = await httpClient.GetFromJsonAsync<ClimateRecord[]>(url);
        return result!;
    }
}