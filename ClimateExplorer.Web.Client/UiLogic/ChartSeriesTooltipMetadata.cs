namespace ClimateExplorer.Web.UiLogic;

/// <summary>
/// The last-30-years, full-period, and early-period reference stats for a single chart series,
/// used by the chart's external tooltip to show how far the hovered value is from each. See
/// ChartTooltipMetadataBuilder.
/// </summary>
public record ChartSeriesTooltipMetadata
{
    public required ChartTooltipPeriod Last30 { get; init; }
    public required ChartTooltipPeriod FullPeriod { get; init; }
    public required ChartTooltipPeriod Early { get; init; }
}
