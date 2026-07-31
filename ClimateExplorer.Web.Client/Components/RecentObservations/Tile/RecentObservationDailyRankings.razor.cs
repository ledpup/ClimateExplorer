namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationDailyRankings
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RecentObservationRankingsViewModel> Metrics { get; set; } = [];
}
