namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

public interface IRecentObservationsCalculator
{
    RecentObservationsTabResult Calculate(
        Location location,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options);
}
