namespace ClimateExplorer.Web.UiLogic;

/// <summary>
/// The output of <see cref="ChartDisplayBuilder"/>: the Chart.js datasets, the tooltip metadata aligned with
/// them, and the series note drawn beside the legend (null when there's none).
/// </summary>
public sealed record ChartDisplayBuildResult(List<ChartJsDataset> Datasets, List<ChartTooltipSeriesInfo> TooltipMetadata, string? SeriesNote);
