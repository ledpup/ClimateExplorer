namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.Model;

public class SeriesWithData
{
    public required ChartSeriesDefinition ChartSeries { get; set; }
    public required DataSet SourceDataSet { get; set; }
    public DataSet? PreProcessedDataSet { get; set; }
    public DataSet? ProcessedDataSet { get; set; }
}
