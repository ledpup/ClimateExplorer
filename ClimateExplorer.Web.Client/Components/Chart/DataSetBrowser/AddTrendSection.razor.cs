namespace ClimateExplorer.Web.Client.Components.Chart.DataSetBrowser;

using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Lets the user add a trend to a series that's already on the chart, from inside the data set
/// browser side panel - a quicker path than scrolling down to that series' own controls below the
/// chart. Shared by <see cref="LocalDataSetBrowser"/> and <see cref="GlobalDataSetBrowser"/>, each
/// supplying its own filtered <see cref="EligibleSeries"/> (a series' own location-vs-region
/// origin decides which of the two it belongs to - see <see cref="ChartSeriesDefinition.IsGlobalSeries"/>).
/// </summary>
public partial class AddTrendSection
{
    private Guid selectedSeriesId;

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<ChartSeriesDefinition> EligibleSeries { get; set; } = [];

    [Parameter]
    public IReadOnlyList<SeriesWithData>? SeriesWithData { get; set; }

    /// <summary>Shown in place of the series picker when <see cref="EligibleSeries"/> is empty.</summary>
    [Parameter]
    [EditorRequired]
    public string EmptyMessage { get; set; } = string.Empty;

    /// <summary>Raised after a trend is added to, or removed from, one of <see cref="EligibleSeries"/> - triggers a chart rebuild.</summary>
    [Parameter]
    public EventCallback OnTrendsChanged { get; set; }

    /// <summary>
    /// The series a trend was just added to from this component, so its <see cref="ChartSeriesTrendControls"/>
    /// can be shown inline for immediate tuning. Cleared when the picker's selection changes, or the
    /// pending trend is removed - it's a real, already-committed entry in that series' <c>Trends</c>
    /// list either way, not a draft; clearing this just stops showing its controls here.
    /// </summary>
    private ChartSeriesDefinition? PendingSeries { get; set; }

    private int PendingTrendIndex { get; set; } = -1;

    private ChartSeriesDefinition? SelectedSeries =>
        EligibleSeries.FirstOrDefault(x => x.Id == selectedSeriesId) ?? EligibleSeries.FirstOrDefault();

    private void OnSeriesSelected(Guid id)
    {
        selectedSeriesId = id;
        PendingSeries = null;
        PendingTrendIndex = -1;
    }

    private async Task OnAddTrendClicked()
    {
        var series = SelectedSeries!;
        series.Trends.Add(new ChartSeriesTrendRequest());

        PendingSeries = series;
        PendingTrendIndex = series.Trends.Count - 1;

        await OnTrendsChangedInternal();
    }

    private async Task OnRemovePendingTrend()
    {
        PendingSeries!.Trends.RemoveAt(PendingTrendIndex);
        PendingSeries = null;
        PendingTrendIndex = -1;

        await OnTrendsChangedInternal();
    }

    private async Task OnTrendsChangedInternal()
    {
        await OnTrendsChanged.InvokeAsync();
    }

    /// <summary>The fitted result matching the pending trend, or null immediately after "Add trend", before the next rebuild completes.</summary>
    private ChartSeriesTrend? GetPendingTrendResult()
    {
        var trends = SeriesWithData?.FirstOrDefault(x => x.ChartSeries.Id == PendingSeries!.Id)?.Trends;

        return trends is null || PendingTrendIndex >= trends.Count ? null : trends[PendingTrendIndex];
    }

    /// <summary>"Trend 1", "Trend 2" etc. - always shown, matching <c>ChartSeriesView</c>'s own trend controls.</summary>
    private string GetPendingSlotLabel()
    {
        return $"Trend {PendingTrendIndex + 1}";
    }
}
