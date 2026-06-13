namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Web.Client.UiModel;

public interface IRecentObservationsService
{
    Task<RecentObservationsTabResult> GetTemperatureRecords(Guid locationId);
    Task<RecentObservationsTabResult> GetPrecipitationRecords(Guid locationId);
}
