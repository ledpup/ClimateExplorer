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
/// <remarks>
/// Always renders the selected series' actual <c>Trends</c> list - never tracks "the trend I just
/// added" as separate local state. An earlier version did that (a single "pending" slot, cleared on
/// selection change) and broke two ways: reopening this panel, or re-selecting a series, showed only
/// an "Add trend" button with no sign that series already had trends from an earlier visit or from
/// the main series list below the chart; and once a series reached the three-trend cap it dropped out
/// of the (cap-filtered) picker entirely, hiding the controls for the trend that had just pushed it
/// there. Deriving straight from <c>SelectedSeries.Trends</c> each render - the same pattern
/// <c>ChartSeriesView</c> uses for its own trend loop - is always in sync with what's really on the
/// series, however it got there.
/// </remarks>
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

    private ChartSeriesDefinition? SelectedSeries =>
        EligibleSeries.FirstOrDefault(x => x.Id == selectedSeriesId) ?? EligibleSeries.FirstOrDefault();

    private void OnSeriesSelected(Guid id)
    {
        selectedSeriesId = id;
    }

    private async Task OnAddTrendClicked()
    {
        SelectedSeries!.Trends.Add(new ChartSeriesTrendRequest());

        await OnTrendsChangedInternal();
    }

    private async Task OnRemoveTrendClicked(int index)
    {
        SelectedSeries!.Trends.RemoveAt(index);

        await OnTrendsChangedInternal();
    }

    private async Task OnTrendsChangedInternal()
    {
        await OnTrendsChanged.InvokeAsync();
    }

    /// <summary>The fitted result matching <c>SelectedSeries.Trends[index]</c>, or null when the build hasn't produced it yet (e.g. immediately after "Add trend", before the next rebuild completes).</summary>
    private ChartSeriesTrend? GetTrend(int index)
    {
        var trends = SeriesWithData?.FirstOrDefault(x => x.ChartSeries.Id == SelectedSeries!.Id)?.Trends;

        return trends is null || index >= trends.Count ? null : trends[index];
    }

    /// <summary>"Trend 1", "Trend 2" etc. - always shown, matching <c>ChartSeriesView</c>'s own trend controls.</summary>
    private string GetTrendSlotLabel(int index)
    {
        return $"Trend {index + 1}";
    }
}
