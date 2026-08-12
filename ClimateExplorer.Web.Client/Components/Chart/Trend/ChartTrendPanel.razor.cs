namespace ClimateExplorer.Web.Client.Components.Chart.Trend;

using System.Globalization;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.AspNetCore.Components;

/// <summary>
/// The chart's "About trends" side panel: the shared overview of what a trend is, plus the full
/// statistical breakdown for all three periods - including the ones that weren't significant enough
/// to be offered in the dropdown, which is where a user goes to find out why.
/// </summary>
public partial class ChartTrendPanel
{
    private const string OverviewTabName = "Overview";

    private SidePanel? sidePanel;
    private string selectedTab = OverviewTabName;

    [Parameter]
    public ChartSeriesTrend? Trend { get; set; }

    private IReadOnlyList<ChartSeriesTrendWindowResult> Windows => Trend?.Windows ?? [];

    private ChartSeriesTrendWindowResult? SelectedWindow =>
        Windows.FirstOrDefault(x => TabName(x.Window) == selectedTab);

    public Task Show()
    {
        return sidePanel!.ShowAsync();
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
                + $"{TrendFormatting.FormatPerDecadeValue(window.Regression, unit)}, but it isn't statistically significant, "
                + "so this period isn't offered as a trend to display.";
        }

        var isDisplayed = Trend.Projection?.Window == window.Window;

        return $"{TrendWindowLabel.Get(window.Window)} ({years}): {TrendFormatting.FormatPerDecadeValue(window.Regression, unit)}"
            + (isDisplayed ? ", currently displayed on the chart." : ", available to display on the chart.");
    }

    private IReadOnlyList<TrendStatSection> BuildSections(ChartSeriesTrendWindowResult window)
    {
        return TrendStatSectionBuilder.Build(Trend!.Subject, window.Regression, window.Points, Trend.Points);
    }
}
