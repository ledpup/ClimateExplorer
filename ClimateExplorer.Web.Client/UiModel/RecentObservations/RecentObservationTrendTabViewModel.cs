namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record RecentObservationTrendTabViewModel : RecentObservationExpandedTabViewModel
{
    /// <summary>
    /// Trend metrics involve linear regressions and are comparatively expensive to compute,
    /// so building them is deferred until a tile's Trend tab is actually viewed.
    /// </summary>
    public required Lazy<IReadOnlyList<RecentObservationTrendViewModel>> MetricsFactory { get; init; }

    public IReadOnlyList<RecentObservationTrendViewModel> Metrics => MetricsFactory.Value;
}
