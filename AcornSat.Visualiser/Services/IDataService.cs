using AcornSat.Core;
using static AcornSat.Core.Enums;

public interface IDataService
{
    Task<IEnumerable<DataSetDefinition>> GetDataSetDefinitions();
    Task<IEnumerable<Location>> GetLocations(string dataSetName = null);

    Task<IEnumerable<DataSet>> GetDataSet(DataType dataType, DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year = null, float? threshold = .7f, short? dayGrouping = 14, bool? relativeToAverage = false);

    Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, MeasurementType measurementType, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .7f, float locationGroupingThreshold = .7f);

    Task<IEnumerable<EnsoMetaData>> GetEnsoMetaData();
    Task<IEnumerable<DataRecord>> GetEnso(EnsoIndex index, DataResolution resolution, string measure);
}
