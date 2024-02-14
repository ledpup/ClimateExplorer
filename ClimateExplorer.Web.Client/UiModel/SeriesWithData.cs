namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.Model;

public class SeriesWithData
{
    required public ChartSeriesDefinition ChartSeries { get; set; }
    required public DataSet SourceDataSet { get; set; }
    public DataSet? PreProcessedDataSet { get; set; }
    public DataSet? ProcessedDataSet { get; set; }
}
