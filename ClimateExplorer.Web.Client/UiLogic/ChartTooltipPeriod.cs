namespace ClimateExplorer.Web.UiLogic;

/// <summary>A single reference period (e.g. "last 30 years") shown under the chart tooltip's anomaly table.</summary>
public record ChartTooltipPeriod(int FirstYear, int LastYear, int MissingYears, double Average);
