namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

/// <summary>
/// A named, ordered expanded-tile tab. Different tab types can use different
/// metric row shapes while sharing the same selection and toggle UI.
/// </summary>
public abstract record RecentObservationExpandedTabViewModel
{
    public MetricGroupKey? Key { get; init; }
    public string Title { get; init; } = string.Empty;
}
