namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;

public interface IRecentObservationsService
{
    Task<RecentObservationsTabResult> GetTemperatureRecords(Location location, int previousDayCount, int previousMonthCount, int previousSeasonCount, DateOnly? referenceDate = null);
    Task<RecentObservationsTabResult> GetPrecipitationRecords(Location location, int previousDayCount, int previousMonthCount, int previousSeasonCount, DateOnly? referenceDate = null);
}
