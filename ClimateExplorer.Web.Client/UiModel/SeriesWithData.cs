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
}
