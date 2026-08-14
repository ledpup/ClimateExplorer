namespace ClimateExplorer.Web.Client.Components.Chart.Trend;

using System.Globalization;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.AspNetCore.Components;

/// <summary>
/// The chart's "About trends" side panel: the shared overview of what a trend is, plus the full
/// statistical breakdown for every period - including the ones that weren't significant enough to
/// be offered in the dropdown, which is where a user goes to find out why.
/// </summary>
public partial class ChartTrendPanel
{
    private const string OverviewTabName = "Overview";
    private const string CurvedTrendsTabName = "About curved trends";

    private SidePanel? sidePanel;
    private string selectedTab = OverviewTabName;

    [Parameter]
    public ChartSeriesTrend? Trend { get; set; }

    private IReadOnlyList<ChartSeriesTrendWindowResult> Windows => Trend?.Windows ?? [];

    /// <summary>
    /// The curved-trends explainer only applies - and only earns a tab - when the series is actually
    /// fitted as a curve. A series left on Linear sees exactly the tab set it always has.
    /// </summary>
    private bool ShowCurvedTrendsTab => Trend?.RegressionType is TrendRegressionType.Quadratic or TrendRegressionType.Cubic;

    private ChartSeriesTrendWindowResult? SelectedWindow =>
        Windows.FirstOrDefault(x => TabName(x.Window) == selectedTab);

    public Task Show()
    {
        return sidePanel!.ShowAsync();
    }

    /// <summary>
    /// Falls back to Overview if the curved-trends tab was selected and the series' regression type
    /// then changed away from Quadratic/Cubic (its tab button disappears, but the panel can still be
    /// open on it) - the same "stay on a tab that no longer exists" hazard the window tabs already
    /// have if a previously-significant window drops out, just guarded here since it's new.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (selectedTab == CurvedTrendsTabName && !ShowCurvedTrendsTab)
        {
            selectedTab = OverviewTabName;
        }
    }

    private static string TabName(TrendWindow window) => window.ToString();

    private void OnTabChanged(string tabName)
    {
        selectedTab = tabName;
    }

    private string DescribeWindow(ChartSeriesTrendWindowResult window)
    {
        var years = $"{window.FirstYear.ToString(CultureInfo.InvariantCulture)}-{window.LastYear.ToString(CultureInfo.InvariantCulture)}";
        var unit = Trend!.Subject.Unit;

        if (!window.IsSignificant)
        {
            return $"{TrendWindowLabel.Get(window.Window)} ({years}): the fitted rate is "
                + $"{TrendFormatting.FormatPerDecadeValue(window.Regression, window.Regression.Input.MaximumX, unit)}, but it isn't statistically significant, "
                + "so this period isn't offered as a trend to display.";
        }

        var isDisplayed = Trend.Projection?.Window == window.Window;

        return $"{TrendWindowLabel.Get(window.Window)} ({years}): {TrendFormatting.FormatPerDecadeValue(window.Regression, window.Regression.Input.MaximumX, unit)}"
            + (isDisplayed ? ", currently displayed on the chart." : ", available to display on the chart.");
    }

    private IReadOnlyList<TrendStatSection> BuildSections(ChartSeriesTrendWindowResult window)
    {
        return TrendStatSectionBuilder.Build(Trend!.Subject, window.Regression, window.Points, Trend.Points);
    }
}
