using AcornSat.Core;

public interface IDataService
{
    Task<IEnumerable<YearlyAverageTemps>> GetTemperatures(string temperatureType, Guid locationId);
    Task<IEnumerable<Location>> GetLocations();
    Task<IEnumerable<ReferenceData>> GetMeiV2(string measure);
}
