namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationAverageTabViewModel : RecentObservationExpandedTabViewModel
{
    public IReadOnlyList<RecentObservationVariationViewModel> Metrics { get; init; } = [];
}
