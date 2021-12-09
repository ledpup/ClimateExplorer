using System.Net.Http.Json;
public class TemperatureDataService : ITemperatureDataService
{
        
    private readonly HttpClient _httpClient;

    public TemperatureDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<YearlyAverageTemps>> GetTemperatureData(string locationId)
    {
        return await _httpClient.GetFromJsonAsync<YearlyAverageTemps[]>($"temperature/{locationId}");
    }

    public async Task<IEnumerable<Location>> GetLocations()
    {
        return await _httpClient.GetFromJsonAsync<Location[]>("location");
    }
}