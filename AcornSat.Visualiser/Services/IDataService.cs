using AcornSat.Core;
using static AcornSat.Core.Enums;

public interface IDataService
{
    Task<IEnumerable<Location>> GetLocations();

    Task<IEnumerable<DataSet>> GetTemperatures(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year = null, float? threshold = .7f, short? dayGrouping = 14);

    Task<IEnumerable<EnsoMetaData>> GetEnsoMetaData();
    Task<IEnumerable<ReferenceData>> GetEnso(EnsoIndex index, DataResolution resolution, string measure);
}
