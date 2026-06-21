namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.UiModel;

/// <summary>
/// The inputs required by <see cref="ChartOptionsFactory"/> to build the Chart.js options object.
/// </summary>
public sealed record ChartOptionsRequest
{
    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required BinGranularities BinGranularity { get; init; }

    public required bool IsMobileDevice { get; init; }

    /// <summary>Gets the prepared series, used to derive the global per-axis min/max range.</summary>
    public required IReadOnlyList<SeriesWithData> SeriesWithData { get; init; }

    /// <summary>Gets the chart series definitions, used to derive the set of y axes and their labels.</summary>
    public required IReadOnlyList<ChartSeriesDefinition> Series { get; init; }

    public required IReadOnlyDictionary<string, bool> AxesScaleToZero { get; init; }
}
