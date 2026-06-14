namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;

public interface IRecentObservationsService
{
    Task<RecentObservationsTabResult> GetTemperatureRecords(Location location, int previousMonthCount, int previousSeasonCount);
    Task<RecentObservationsTabResult> GetPrecipitationRecords(Location location, int previousMonthCount, int previousSeasonCount);
}
