namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public record RecentObservationRecordsTabViewModel : RecentObservationExpandedTabViewModel
{
    public IReadOnlyList<RecentObservationRecordsViewModel> Metrics { get; init; } = [];
}
