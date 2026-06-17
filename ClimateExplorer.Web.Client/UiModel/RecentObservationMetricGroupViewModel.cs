namespace ClimateExplorer.Web.Client.UiModel;

/// <summary>
/// A named, ordered set of metrics shown as one state of the expanded-tile toggle
/// (for example "Period" or "Daily Extremes"). Additional groups can be added
/// without changing the tile UI.
/// </summary>
public sealed record RecentObservationMetricGroupViewModel
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<RecentObservationMetricViewModel> Metrics { get; init; } = [];
}
