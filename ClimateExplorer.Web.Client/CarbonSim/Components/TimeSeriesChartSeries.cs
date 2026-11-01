namespace ClimateExplorer.Web.Client.CarbonSim.Components;

/// <summary>
/// A line on <see cref="TimeSeriesChart"/>. Hidden lines can be shown from the legend. A dashed line stays visible where
/// it runs on top of another.
/// </summary>
public sealed record TimeSeriesChartSeries(string Label, string Colour, bool Hidden = false, bool Dashed = false);
