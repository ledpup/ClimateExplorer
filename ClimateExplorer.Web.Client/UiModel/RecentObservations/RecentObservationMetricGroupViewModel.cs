namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

/// <summary>
/// A named, ordered set of metrics shown as one state of the expanded-tile toggle
/// (for example "7 Days" or "Daily extremes"). Additional groups can be added
/// without changing the tile UI.
/// </summary>
public sealed record RecentObservationMetricGroupViewModel
{
    public MetricGroupKey? Key { get; init; }
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<RecentObservationRecordsViewModel> Metrics { get; init; } = [];
}