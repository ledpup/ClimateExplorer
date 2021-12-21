using AcornSat.Core;
using System.Net.Http.Json;
public class DataService : IDataService
{   
    private readonly HttpClient _httpClient;

    public DataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<YearlyAverageTemps>> GetTemperatures(string temperatureType, Guid locationId)
    {
        return await _httpClient.GetFromJsonAsync<YearlyAverageTemps[]>($"temperature/{temperatureType}/{locationId}");
    }

    public async Task<IEnumerable<Location>> GetLocations()
    {
        return await _httpClient.GetFromJsonAsync<Location[]>("location");
    }

    public async Task<IEnumerable<ReferenceData>> GetMeiV2(string measure)
    {
        return await _httpClient.GetFromJsonAsync<ReferenceData[]>($"reference/meiv2/{measure}");
    }
}