namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationTrend
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RecentObservationTrendViewModel> Metrics { get; set; } = [];
}
