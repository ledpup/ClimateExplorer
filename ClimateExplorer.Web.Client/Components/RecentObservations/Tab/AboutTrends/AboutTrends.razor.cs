namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.AspNetCore.Components;

public partial class AboutTrends
{
    private const string OverviewTabName = "Overview";

    // The tile's headline, so the statistics open on the trend the user was just looking at.
    private const TrendWindow DefaultWindow = TrendWindow.Last30;

    private SidePanel? sidePanel;
    private string selectedMetricKey = OverviewTabName;
    private TrendWindow selectedWindow = DefaultWindow;

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

    // The tile's order, then the full period. Since 1961 only when the metric's record is eligible for it.
    private static IReadOnlyList<TrendWindow> GetWindows(RecentObservationTrendViewModel metric)
    {
        return metric.Since1961Trend is null
            ? [TrendWindow.Last30, TrendWindow.FirstHalf, TrendWindow.Full]
            : [TrendWindow.Last30, TrendWindow.Since1961, TrendWindow.FirstHalf, TrendWindow.Full];
    }

    private static IReadOnlyList<TrendStatSection> BuildSections(RecentObservationTrendViewModel metric, TrendWindow window)
    {
        var trend = GetTrend(metric, window);
        if (trend is null)
        {
            return [];
        }

        var (points, _) = GetWindow(metric, window);

        return TrendStatSectionBuilder.Build(
            new TrendStatSubject(metric.Label, metric.Unit),
            trend,
            points,
            metric.FullPeriodPoints);
    }

    private static PolynomialRegressionResult? GetTrend(RecentObservationTrendViewModel metric, TrendWindow window)
    {
        return window switch
        {
            TrendWindow.Full => metric.FullPeriodTrend,
            TrendWindow.Last30 => metric.RecentTrend,
            TrendWindow.Since1961 => metric.Since1961Trend,
            TrendWindow.FirstHalf => metric.FirstHalfTrend,
            _ => throw new NotImplementedException(),
        };
    }

    private static (IReadOnlyList<DataPoint> Points, string Label) GetWindow(RecentObservationTrendViewModel metric, TrendWindow window)
    {
        return window switch
        {
            TrendWindow.Full => (metric.FullPeriodPoints, "Full recordset"),
            TrendWindow.Last30 => (metric.RecentTrendPoints, "Last 30 years"),
            TrendWindow.Since1961 => (metric.Since1961TrendPoints, TrendWindowLabel.Get(TrendWindow.Since1961)),
            TrendWindow.FirstHalf => (metric.FirstHalfTrendPoints, "First half of recordset"),
            _ => throw new NotImplementedException(),
        };
    }

    private void OnMetricTabChanged(string tabName)
    {
        selectedMetricKey = tabName;
        selectedWindow = DefaultWindow;
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
            Unit = metric.Unit,
        });
    }
}
