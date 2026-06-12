namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Web.Client.UiModel;

public interface ILatestRecordsService
{
    Task<LatestRecordsTabResult> GetTemperatureRecords(Guid locationId);
    Task<LatestRecordsTabResult> GetPrecipitationRecords(Guid locationId);
}
