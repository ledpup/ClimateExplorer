using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.UiModel;
using ClimateExplorer.Core.DataPreparation;
using static ClimateExplorer.Core.Enums;
using ClimateExplorer.Visualiser.Services;

public interface IDataService
{
    Task<ApiMetadataModel> GetAbout();
    Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions();
    Task<IEnumerable<Location>> GetLocations(string? dataSetName = null, bool includeNearbyLocations = false, bool includeWarmingMetrics = false);
    Task<DataSet> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment? dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId = null, short? year = null, short? dayGrouping = 14, float? dayGroupingThreshold = .7f);

    Task<DataSet> PostDataSet(SimpleRequest simpleRequest);
    Task<DataSet> PostDataSet(CompoundSeriesTypes compoundSeriesTypes, SimpleRequest[] simpleRequest);
    Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .7f, float locationGroupingThreshold = .7f);
}
