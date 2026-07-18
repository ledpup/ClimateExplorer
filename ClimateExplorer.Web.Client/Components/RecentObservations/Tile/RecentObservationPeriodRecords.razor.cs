namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationPeriodRecords
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RecentObservationRecordsViewModel> Metrics { get; set; } = [];
}
