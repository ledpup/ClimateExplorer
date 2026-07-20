namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationTrend
{
    private AboutTrends? aboutTrends;

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RecentObservationTrendViewModel> Metrics { get; set; } = [];

    [Parameter]
    public EventCallback<TrendDownloadRequest> OnDownloadRequested { get; set; }

    private Task ShowAboutTrends()
    {
        return aboutTrends!.Show();
    }
}
