using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Core.DataPreparation;
using static ClimateExplorer.Core.Enums;
public interface IDataService
{
    Task<ApiMetadataModel> GetAbout();
    Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions();
    Task<IEnumerable<Location>> GetLocations(string? dataSetName = null, bool includeNearbyLocations = false, bool includeWarmingIndex = false, bool excludeLocationsWithNullWarmingIndex = true);
    Task<DataSet> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment? dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId = null, short? year = null, short? dayGrouping = 14, float? dayGroupingThreshold = .7f);
    Task<DataSet> PostDataSet(
        BinGranularities binGranularity,
        ContainerAggregationFunctions binAggregationFunction,
        ContainerAggregationFunctions bucketAggregationFunction,
        ContainerAggregationFunctions cupAggregationFunction,
        SeriesValueOptions seriesValueOption,
        SeriesSpecification[] seriesSpecifications,
        SeriesDerivationTypes seriesDerivationType,
        float requiredBinDataProportion,
        float requiredBucketDataProportion,
        float requiredCupDataProportion,
        int cupSize,
        SeriesTransformations seriesTransformation,
        short? year = null);
    Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .7f, float locationGroupingThreshold = .7f);

    Task<Dictionary<string, string>> GetCountries();
}
