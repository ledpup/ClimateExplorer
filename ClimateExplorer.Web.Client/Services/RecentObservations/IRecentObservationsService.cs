namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;

public interface IRecentObservationsService
{
    Task<RecentObservationsDataSet> LoadTemperatureData(Location location);

    Task<RecentObservationsDataSet> LoadPrecipitationData(Location location);

    RecentObservationsTabResult Calculate(
        Location location,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options);

    Task<RecentObservationsTabResult> GetTemperatureRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset);

    Task<RecentObservationsTabResult> GetPrecipitationRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset);
}
