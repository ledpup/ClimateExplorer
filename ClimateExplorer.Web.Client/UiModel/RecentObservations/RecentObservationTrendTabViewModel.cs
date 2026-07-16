namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationTrendTabViewModel : RecentObservationExpandedTabViewModel
{
    public IReadOnlyList<RecentObservationTrendViewModel> Metrics { get; init; } = [];
}
