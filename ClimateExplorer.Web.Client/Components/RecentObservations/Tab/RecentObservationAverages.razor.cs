namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationAverages
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RecentObservationVariationViewModel> Metrics { get; set; } = [];
}
