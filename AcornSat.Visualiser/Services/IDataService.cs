using AcornSat.Core;
using static AcornSat.Core.Enums;

public interface IDataService
{
    Task<IEnumerable<Location>> GetLocations();

    Task<IEnumerable<DataSet>> GetTemperatures(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year = null);

    Task<IEnumerable<EnsoMetaData>> GetEnsoMetaData();
    Task<IEnumerable<ReferenceData>> GetEnso(EnsoIndex index, DataResolution resolution, string measure);
}
