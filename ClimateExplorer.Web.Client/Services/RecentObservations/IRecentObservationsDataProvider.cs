namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using static ClimateExplorer.Core.Enums;

public interface IRecentObservationsDataProvider
{
    Task<RecentObservationsDataSet> LoadTemperatureData(Location location, DataAdjustment? preferredAdjustment = DataAdjustment.Adjusted);

    Task<RecentObservationsDataSet> LoadPrecipitationData(Location location);

    Task<RecentObservationsDataSet> LoadData(Guid contextId, ObservationDomain domain, DataAdjustment? preferredAdjustment = null);
}
