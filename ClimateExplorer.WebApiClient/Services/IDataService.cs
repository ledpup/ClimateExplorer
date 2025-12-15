namespace ClimateExplorer.WebApiClient.Services;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;
public interface IDataService
{
    Task<ApiMetadataModel> GetAbout();
    Task<IEnumerable<DataSetDefinitionViewModel>> GetDataSetDefinitions();
    Task<IEnumerable<Location>> GetLocations(bool permitCreateCache = true);
    Task<IEnumerable<LocationDistance>> GetNearbyLocations(Guid locationId);
    Task<Location> GetLocationByPath(string path);
    Task<IEnumerable<Region>> GetRegions();
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
    Task<Dictionary<string, string>> GetCountries();
    Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable();
    Task<IEnumerable<ClimateRecord>> GetClimateRecords(Guid locationId);
}
