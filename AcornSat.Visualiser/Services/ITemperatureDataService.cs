public interface ITemperatureDataService
{
    Task<IEnumerable<YearlyAverageTemps>> GetTemperatureData(string temperatureType, Guid locationId);
    Task<IEnumerable<Location>> GetLocations();
}
