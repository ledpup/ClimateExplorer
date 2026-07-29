namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;

public interface IRecentObservationsCalculator
{
    RecentObservationsTabResult Calculate(
        double? latitude,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options);
}
