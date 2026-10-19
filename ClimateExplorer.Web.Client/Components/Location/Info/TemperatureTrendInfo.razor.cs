namespace ClimateExplorer.Web.Client.Components.Info;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.AspNetCore.Components;

/// <summary>
/// The dashboard temperature trend field's modal. It recalculates the full-period, last-30-years and
/// early-period trends from the climate stripe's yearly data, so it can explain each one and offer the
/// full statistical breakdown through "About trends".
/// </summary>
public partial class TemperatureTrendInfo
{
    private const string Unit = "°C";

    private TemperatureTrendAboutPanel? aboutPanel;

    [Parameter]
    public Location? Location { get; set; }

    /// <summary>
    /// The climate stripe's yearly temperature data - the same series the dashboard field's
    /// pre-calculated trends were fitted to.
    /// </summary>
    [Parameter]
    public DataSet? DataSet { get; set; }

    private LocationTrendDetails? Details { get; set; }

    protected override void OnParametersSet()
    {
        Details = DataSet is null ? null : LocationTrendDetailsBuilder.Build(DataSet.DataRecords);
    }

    private static string DescribePeriod(LocationTrendWindow window)
    {
        var description = $"{window.FirstYear}–{window.LastYear}, {window.Points.Count} complete years";

        return window.Window == TrendWindow.FirstHalf
            ? $"the first half of the record, {description}"
            : description;
    }

    private static string FormatRate(LocationTrendWindow window) =>
        TrendFormatting.FormatPerDecadeValue(window.Regression, window.Regression.Input.MaximumX, Unit);

    // FormatPValue gives "< 0.0001" for very small values, which reads as "p < 0.0001", not "p = < 0.0001".
    private static string FormatPValue(LocationTrendWindow window)
    {
        var pValue = TrendFormatting.FormatPValue(window.Regression.Significance.PValue);

        return pValue.StartsWith('<') ? $"p {pValue}" : $"p = {pValue}";
    }

    private Task ShowAboutTrends()
    {
        return aboutPanel?.Show() ?? Task.CompletedTask;
    }
}
