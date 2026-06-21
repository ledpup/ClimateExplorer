namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using ClimateExplorer.Core.Model;

public interface IRecentObservationsDataProvider
{
    Task<RecentObservationsDataSet> LoadTemperatureData(Location location);

    Task<RecentObservationsDataSet> LoadPrecipitationData(Location location);
}
