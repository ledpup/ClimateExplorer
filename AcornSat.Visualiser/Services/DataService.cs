using AcornSat.Core;
using System.Net.Http.Json;
using static AcornSat.Core.Enums;

public class DataService : IDataService
{   
    private readonly HttpClient _httpClient;

    public DataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<DataSet>> GetDataSet(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, float? threshold = .7f, short? dayGrouping = 14, bool? relativeAverage = true)
    {
        var url = $"dataSet/{resolution}/{measurementType}/{locationId}";
        if (year != null)
        {
            url += $"?year={year}";
        }
        if (threshold != null)
        {
            url += url.Contains("?") ? "&" : "?";
            url += $"threshold={threshold}";
        }
        if (dayGrouping != null)
        {
            url += url.Contains("?") ? "&" : "?";
            url += $"dayGrouping={dayGrouping}";
        }
        if (relativeAverage != null)
        {
            url += url.Contains("?") ? "&" : "?";
            url += $"relativeAverage={relativeAverage}";
        }
        return await _httpClient.GetFromJsonAsync<DataSet[]>(url);
    }

    public async Task<IEnumerable<DataSet>> GetAggregateDataSet(DataResolution resolution, MeasurementType measurementType, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .5f, float locationGroupingThreshold = .5f)
    {
        var url = $"dataSet/{resolution}/{measurementType}?dayGrouping={dayGrouping}&dayGroupingThreshold={dayGroupingThreshold}&locationGroupingThreshold={locationGroupingThreshold}";
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

    public async Task<IEnumerable<Location>> GetLocations(string dataSetName)
    {
        var url = $"/location";
        if (dataSetName != null)
        {
            url += $"?dataSetName={dataSetName}";
        }
        return await _httpClient.GetFromJsonAsync<Location[]>(url);
    }

    public async Task<IEnumerable<EnsoMetaData>> GetEnsoMetaData()
    {
        return await _httpClient.GetFromJsonAsync<EnsoMetaData[]>($"reference/enso-metadata");
    }

    public async Task<IEnumerable<ReferenceData>> GetEnso(EnsoIndex index, DataResolution resolution, string measure)
    {
        return await _httpClient.GetFromJsonAsync<ReferenceData[]>($"reference/enso/{index}/{resolution}?measure={measure}");
    }
}