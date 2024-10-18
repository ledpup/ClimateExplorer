namespace ClimateExplorer.WebApiClient.Services;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;
public interface IDataService
{
    Task<ApiMetadataModel> GetAbout();
    Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions();
    Task<IEnumerable<Location>> GetLocations(Guid? locationId = null);
    Task<Location> GetLocationByPath(string path);
    Task<IEnumerable<Region>> GetRegions();
    Task<DataSet> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment? dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId = null, short? year = null, short? groupingDays = 14, float? groupingThreshold = .7f);
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
        short? year = null,
        DataResolution? minimumDataResolution = null);
    Task<IEnumerable<DataSet>> GetAggregateDataSet(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float? minLatitude, float? maxLatitude, short groupingDays = 14, float groupingThreshold = .7f, float regionThreshold = .7f);
    Task<Dictionary<string, string>> GetCountries();
    Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable();
    Task<IEnumerable<ClimateRecord>> GetClimateRecords(Guid locationId);
}
