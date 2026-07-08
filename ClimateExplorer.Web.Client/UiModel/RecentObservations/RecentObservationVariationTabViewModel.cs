namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationVariationTabViewModel : RecentObservationExpandedTabViewModel
{
    public IReadOnlyList<RecentObservationVariationViewModel> Metrics { get; init; } = [];
}
