using AcornSat.Core;
using static AcornSat.Core.Enums;

public interface IDataService
{
    Task<IEnumerable<YearlyAverageTemps>> GetYearlyTemperatures(MeasurementType measurementType, Guid locationId);
    Task<IEnumerable<DailyTemperatureRecord>> GetDailyTemperatures(MeasurementType measurementType, Guid locationId, int year);
    Task<IEnumerable<Location>> GetLocations();

    Task<IEnumerable<EnsoMetaData>> GetEnsoMetaData();
    Task<IEnumerable<ReferenceData>> GetEnso(EnsoIndex index, DataResolution resolution, string measure);
}
