namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using static ClimateExplorer.Core.Enums;

public interface IRecentObservationsService
{
    Task<RecentObservationsDataSet> LoadTemperatureData(Location location, DataAdjustment? preferredAdjustment = DataAdjustment.Adjusted);

    Task<RecentObservationsDataSet> LoadPrecipitationData(Location location);

    Task<RecentObservationsDataSet> LoadData(Guid contextId, ObservationDomain domain, DataAdjustment? preferredAdjustment = null);

    RecentObservationsTabResult Calculate(
        Location location,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options);

    RecentObservationsTabResult Calculate(
        double? latitude,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options);

    Task<RecentObservationsTabResult> GetTemperatureRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        int previousYearCount = 0,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset);

    Task<RecentObservationsTabResult> GetPrecipitationRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        int previousYearCount = 0,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset);
}
