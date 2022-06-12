using AcornSat.Core;
using AcornSat.Core.Model;
using AcornSat.Core.ViewModel;
using static AcornSat.Core.Enums;
public interface IDataService
{
    Task<ApiMetadataModel> GetAbout();
    Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions();
    Task<IEnumerable<Location>> GetLocations(string? dataSetName = null, bool includeNearbyLocations = false, bool includeWarmingMetrics = false);
    Task<IEnumerable<DataSet>> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId = null, short? year = null, short? dayGrouping = 14, float? dayGroupingThreshold = .7f);
    Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .7f, float locationGroupingThreshold = .7f);
}
