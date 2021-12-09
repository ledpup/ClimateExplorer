public interface ITemperatureDataService
{
    Task<IEnumerable<YearlyAverageTemps>> GetTemperatureData(string locationId);
    Task<IEnumerable<Location>> GetLocations();
}
