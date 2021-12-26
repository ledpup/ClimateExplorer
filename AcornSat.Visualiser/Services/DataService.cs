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

    public async Task<IEnumerable<YearlyAverageTemps>> GetYearlyTemperatures(MeasurementType measurementType, Guid locationId)
    {
        return await _httpClient.GetFromJsonAsync<YearlyAverageTemps[]>($"temperature/yearly/{measurementType}/{locationId}");
    }

    public async Task<IEnumerable<DailyTemperatureRecord>> GetDailyTemperatures(MeasurementType measurementType, Guid locationId, int year)
    {
        return await _httpClient.GetFromJsonAsync<DailyTemperatureRecord[]>($"temperature/daily/{measurementType}/{locationId}/{year}");
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