namespace ClimateExplorer.Web.UiLogic;

/// <summary>
/// Everything the chart's external tooltip needs to render one series' row: the trimmed
/// "Location | Data type | Unit" label, the number of decimal places to round the value and any
/// anomaly figures to (per Enums.UnitOfMeasureRounding), and - when available - its anomaly
/// reference periods. See ChartTooltipMetadataBuilder.
/// </summary>
public record ChartTooltipSeriesInfo
{
    public required string Label { get; init; }
    public required int Rounding { get; init; }
    public ChartSeriesTooltipMetadata? Anomaly { get; init; }
}
