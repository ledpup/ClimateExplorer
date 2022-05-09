using AcornSat.Core;
using AcornSat.Core.ViewModel;
using static AcornSat.Core.Enums;
public interface IDataService
{
    Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions();
    Task<IEnumerable<Location>> GetLocations(string dataSetName = null);

    Task<IEnumerable<DataSet>> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, Guid locationId, AggregationMethod? aggregationMethod, short? year = null, short? dayGrouping = 14, float? dayGroupingThreshold = .7f);

    Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .7f, float locationGroupingThreshold = .7f);

    Task<IEnumerable<EnsoMetaData>> GetEnsoMetaData();
    Task<IEnumerable<DataRecord>> GetEnso(EnsoIndex index, DataResolution resolution, string measure);
}
