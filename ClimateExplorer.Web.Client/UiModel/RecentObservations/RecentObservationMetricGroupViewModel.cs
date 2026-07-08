namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

/// <summary>
/// A named, ordered records tab shown as one state of the expanded-tile toggle.
/// Kept as the existing type name while expanded tabs are generalized beyond
/// record/rank metrics.
/// </summary>
public sealed record RecentObservationMetricGroupViewModel : RecentObservationRecordsTabViewModel
{
}
