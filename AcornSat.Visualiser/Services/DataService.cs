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

    public async Task<IEnumerable<DataSet>> GetTemperatures(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year)
    {
        var url = $"temperature/{resolution}/{measurementType}/{locationId}";
        if (year != null)
        {
            url += $"?year={year}";
        }
        return await _httpClient.GetFromJsonAsync<DataSet[]>(url);
    }

    public async Task<IEnumerable<Location>> GetLocations()
    {
        return await _httpClient.GetFromJsonAsync<Location[]>("location");
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