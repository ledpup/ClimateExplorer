namespace ClimateExplorer.Web.Client.CarbonSim.Components;

/// <summary>A line on <see cref="GasChart"/>. Hidden lines can be shown from the legend.</summary>
public sealed record GasChartSeries(string Label, string Colour, bool Hidden);
