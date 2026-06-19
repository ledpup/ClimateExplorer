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
    Task<IEnumerable<LocationDistance>> GetNearbyLocations(Guid locationId, int? take = null, int? skip = null);
    Task<Location> GetLocationByPath(string path);
    Task<Location?> GetLocationById(Guid locationId);
    Task<IEnumerable<Region>> GetRegions();
    Task<DataSet> PostDataSet(
        BinGranularities binGranularity,
        ContainerAggregationFunctions binAggregationFunction,
        ContainerAggregationFunctions bucketAggregationFunction,
        ContainerAggregationFunctions cupAggregationFunction,
        SeriesValueOptions seriesValueOption,
        SeriesSpecification[] seriesSpecifications,
        SeriesDerivationTypes seriesDerivationType,
        float requiredBinDataProportion = 1,
        float requiredBucketDataProportion = 1,
        float requiredCupDataProportion = 0.7f,
        int cupSize = 14,
        SeriesTransformations seriesTransformation = SeriesTransformations.Identity,
        string? customTransformation = null,
        short? year = null,
        DataResolution? minimumDataResolution = null);
    Task<Dictionary<string, string>> GetCountries();
    Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable();
    Task<ClimateRecordsResponse> GetClimateRecords(Guid locationId, DataType dataType = DataType.TempMax, DataAdjustment? dataAdjustment = null, bool ascending = false, int? take = null, int? skip = null, int? month = null, bool monthly = false, int? day = null);
    Task<RecentObservationsResponse> GetRecentObservations(Guid locationId, bool isLocationSupported = false);
}
