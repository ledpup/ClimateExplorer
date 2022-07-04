using AcornSat.Core;
using AcornSat.Core.Model;
using AcornSat.Core.ViewModel;
using AcornSat.Visualiser.Services;
using ClimateExplorer.Core.DataPreparation;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using static AcornSat.Core.Enums;

public class DataService : IDataService
{   
    private readonly HttpClient _httpClient;
    private readonly IDataServiceCache _dataServiceCache;

    public DataService(
        HttpClient httpClient,
        IDataServiceCache dataServiceCache)
    {
        _httpClient = httpClient;
        _dataServiceCache = dataServiceCache;
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
            result = await _httpClient.GetFromJsonAsync<DataSet>(url);

            _dataServiceCache.Put(url, result);
        }

        return result;
    }


    public async Task<DataSet> PostDataSet(BinGranularities binGranularity, BinAggregationFunctions aggregationFunction, Guid dataSetDefinitionId, DataType dataType, DataAdjustment? dataAdjustment, Guid? locationId)
    {
        var response = 
            await _httpClient.PostAsJsonAsync<PostDataSetsRequestBody>(
                "dataset",
                new PostDataSetsRequestBody
                {
                    BinAggregationFunction = aggregationFunction,
                    BinningRule = binGranularity,
                    CupSize = 14,
                    RequiredBinDataProportion = 0.7f,
                    RequiredBucketDataProportion = 0.7f,
                    RequiredCupDataProportion = 0.7f,
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SeriesSpecifications =
                        new SeriesSpecification[]
                        {
                            new SeriesSpecification
                            {
                                DataSetDefinitionId = dataSetDefinitionId,
                                LocationId = locationId,
                                DataAdjustment = dataAdjustment,
                                DataType = dataType
                            }
                        },
                    SeriesTransformation = SeriesTransformations.Identity
                });

        var result = await response.Content.ReadFromJsonAsync<DataSet>();

        return result;
    }

    public async Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .5f, float locationGroupingThreshold = .5f)
    {
        var url = $"dataSet/{dataType}/{resolution}/{dataAdjustment}?dayGrouping={dayGrouping}&dayGroupingThreshold={dayGroupingThreshold}&locationGroupingThreshold={locationGroupingThreshold}";
        if (minLatitude != null)
        {
            url += $"&minLatitude={minLatitude}";
        }
        if (maxLatitude != null)
        {
            url += $"&maxLatitude={maxLatitude}";
        }
        return await _httpClient.GetFromJsonAsync<DataSet[]>(url);
    }

    public async Task<ApiMetadataModel> GetAbout()
    {
        return await _httpClient.GetFromJsonAsync<ApiMetadataModel>("/about");
    }

    public async Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions()
    {
        var url = "/datasetdefinition";
        return await _httpClient.GetFromJsonAsync<DataSetDefinitionViewModel[]>(url);
    }

    public async Task<IEnumerable<Location>> GetLocations(string dataSetName = null, bool includeNearbyLocations = false, bool includeWarmingMetrics = false)
    {
        var url = $"/location";
        if (dataSetName != null)
        {
            url = QueryHelpers.AddQueryString(url, "dataSetName", dataSetName);
        }
        url = QueryHelpers.AddQueryString(url, "includeNearbyLocations", includeNearbyLocations.ToString());
        url = QueryHelpers.AddQueryString(url, "includeWarmingMetrics", includeWarmingMetrics.ToString());
        
        return await _httpClient.GetFromJsonAsync<Location[]>(url);
    }
}