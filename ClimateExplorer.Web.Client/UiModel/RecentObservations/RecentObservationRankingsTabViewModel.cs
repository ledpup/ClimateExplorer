namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public record RecentObservationRankingsTabViewModel : RecentObservationExpandedTabViewModel
{
    public IReadOnlyList<RecentObservationRankingsViewModel> Metrics { get; init; } = [];
}
