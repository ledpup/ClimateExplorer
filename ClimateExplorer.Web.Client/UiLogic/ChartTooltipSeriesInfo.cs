namespace ClimateExplorer.Web.UiLogic;

/// <summary>
/// Everything the chart's external tooltip needs to render one series' row: the trimmed
/// "Location | Data type | Unit" label, and - when available - its anomaly reference periods.
/// See ChartTooltipMetadataBuilder.
/// </summary>
public record ChartTooltipSeriesInfo
{
    public required string Label { get; init; }
    public ChartSeriesTooltipMetadata? Anomaly { get; init; }
}
