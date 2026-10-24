namespace ClimateExplorer.Web.Client.Components.Chart;

/// <summary>
/// The chart-level Chart.js type. Each dataset also carries its own type, so this mostly decides
/// how the x axis is laid out (bar charts offset their labels to sit between grid lines).
/// </summary>
public enum ChartJsType
{
    Line,
    Bar,
}
