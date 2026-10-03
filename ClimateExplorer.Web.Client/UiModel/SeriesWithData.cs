namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel.Trends;

public sealed record SeriesWithData
{
    public required ChartSeriesDefinition ChartSeries { get; set; }
    public required DataSet SourceDataSet { get; set; }
    public DataSet? PreProcessedDataSet { get; set; }
    public DataSet? ProcessedDataSet { get; set; }
    public ChartSeriesDataStatus DataStatus { get; set; } = ChartSeriesDataStatus.Rendered;

    /// <summary>
    /// The fitted trend windows and forward projections for this series - one entry per request in
    /// <see cref="ChartSeriesDefinition.Trends"/>, in the same order, whenever the trend module is
    /// switched on. Derived state, rebuilt on every chart build - the user's trend intent lives on
    /// <see cref="ChartSeries"/>. Index alignment with <c>ChartSeries.Trends</c> is load-bearing:
    /// rendering assigns each trend's colour tier from its position in this list.
    /// </summary>
    public IReadOnlyList<ChartSeriesTrend> Trends { get; set; } = [];

    /// <summary>
    /// The bins whose smoothed value was averaged over fewer slots than
    /// <see cref="ChartSeriesDefinition.SmoothingWindow"/>, keyed by bin id, with the number of slots
    /// actually used. These are the ends of a shrinking moving average. The chart draws them dashed and
    /// the tooltip shows the window size. Empty when the series isn't smoothed or every window was full.
    /// </summary>
    public IReadOnlyDictionary<string, int> ShrunkSmoothingWindows { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// The smoothing window the series' values were averaged over, wherever
    /// <see cref="ShrunkSmoothingWindows"/> doesn't record a smaller one. The tooltip shows it as the
    /// window size. Null when the series isn't smoothed, including when it fell back to unsmoothed data.
    /// </summary>
    public int? SmoothingWindow { get; set; }
}
