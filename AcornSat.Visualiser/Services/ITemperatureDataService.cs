public interface ITemperatureDataService
{
    Task<IEnumerable<YearlyAverageTemps>> GetTemperatureData(string temperatureType, string locationId);
    Task<IEnumerable<Location>> GetLocations();
}
