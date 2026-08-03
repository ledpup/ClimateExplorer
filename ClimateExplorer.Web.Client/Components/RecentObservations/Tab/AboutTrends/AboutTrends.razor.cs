namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class AboutTrends
{
    private const string OverviewTabName = "Overview";

    private SidePanel? sidePanel;
    private string selectedMetricKey = OverviewTabName;
    private TrendWindow selectedWindow = TrendWindow.Full;

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RecentObservationTrendViewModel> Metrics { get; set; } = [];

    [Parameter]
    public EventCallback<TrendDownloadRequest> DownloadRequested { get; set; }

    private RecentObservationTrendViewModel? SelectedMetric => Metrics.FirstOrDefault(x => x.Label == selectedMetricKey);

    public Task Show()
    {
        return sidePanel!.ShowAsync();
    }

    private static IReadOnlyList<TrendStatSection> BuildSections(RecentObservationTrendViewModel metric, TrendWindow window)
    {
        var trend = GetTrend(metric, window);
        return trend is null ? [] : TrendStatSectionBuilder.Build(metric, trend);
    }

    private static LinearRegressionResult? GetTrend(RecentObservationTrendViewModel metric, TrendWindow window)
    {
        return window switch
        {
            TrendWindow.Full => metric.FullPeriodTrend,
            TrendWindow.Recent => metric.RecentTrend,
            TrendWindow.FirstHalf => metric.FirstHalfTrend,
            _ => throw new NotImplementedException(),
        };
    }

    private static (IReadOnlyList<DataPoint> Points, string Label) GetWindow(RecentObservationTrendViewModel metric, TrendWindow window)
    {
        return window switch
        {
            TrendWindow.Full => (metric.FullPeriodPoints, "Full recordset"),
            TrendWindow.Recent => (metric.RecentTrendPoints, "Last 30 years"),
            TrendWindow.FirstHalf => (metric.FirstHalfTrendPoints, "First half of recordset"),
            _ => throw new NotImplementedException(),
        };
    }

    private void OnMetricTabChanged(string tabName)
    {
        selectedMetricKey = tabName;
        selectedWindow = TrendWindow.Full;
    }

    private void SelectWindow(TrendWindow window)
    {
        selectedWindow = window;
    }

    private async Task OnDownloadClicked(RecentObservationTrendViewModel metric, TrendWindow window)
    {
        var (points, windowLabel) = GetWindow(metric, window);
        await DownloadRequested.InvokeAsync(new TrendDownloadRequest
        {
            DataTypeLabel = metric.Label,
            WindowLabel = windowLabel,
            Points = points,
        });
    }
}
