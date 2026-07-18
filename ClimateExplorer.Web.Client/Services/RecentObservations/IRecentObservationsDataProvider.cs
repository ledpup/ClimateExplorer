namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public interface IRecentObservationsDataProvider
{
    Task<RecentObservationsDataSet> LoadTemperatureData(Location location, DataAdjustment? preferredAdjustment = DataAdjustment.Adjusted);

    Task<RecentObservationsDataSet> LoadPrecipitationData(Location location);
}
