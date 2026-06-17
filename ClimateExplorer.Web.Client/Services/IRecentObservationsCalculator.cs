namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;

public interface IRecentObservationsCalculator
{
    RecentObservationsTabResult Calculate(
        Location location,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options);
}
