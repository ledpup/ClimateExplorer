namespace ClimateExplorer.Web.UiLogic;

/// <summary>
/// Everything the chart's external tooltip needs to render one series' row: the trimmed
/// "Location | Data type | Unit" label, the number of decimal places to round the value and any
/// anomaly figures to (per Enums.UnitOfMeasureRounding), when available its anomaly reference
/// periods, and the chart points whose smoothing window was shrunk. See ChartTooltipMetadataBuilder.
/// </summary>
public record ChartTooltipSeriesInfo
{
    public required string Label { get; init; }
    public required int Rounding { get; init; }
    public ChartSeriesTooltipMetadata? Anomaly { get; init; }

    /// <summary>
    /// Chart point index to the number of bins its smoothed value was averaged over, for the points
    /// where that is less than the requested smoothing window. The chart draws these points' line
    /// segments dashed and the tooltip shows the window size next to the value. Null when there are none.
    /// </summary>
    public IReadOnlyDictionary<int, int>? ShrunkWindows { get; init; }

    /// <summary>
    /// Short unit for <see cref="ShrunkWindows"/> sizes (e.g. "yr"), shown as "6-yr avg".
    /// </summary>
    public string? WindowUnit { get; init; }
}
