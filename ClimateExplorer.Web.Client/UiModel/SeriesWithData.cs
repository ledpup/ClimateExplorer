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
    /// The fitted trend windows and forward projection for this series, when the trend module is
    /// switched on. Derived state, rebuilt on every chart build - the user's trend intent lives on
    /// <see cref="ChartSeries"/>.
    /// </summary>
    public ChartSeriesTrend? Trend { get; set; }
}
