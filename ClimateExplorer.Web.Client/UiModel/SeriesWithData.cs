using ClimateExplorer.Core.Model;

namespace ClimateExplorer.Web.UiModel;

public class SeriesWithData
{
    public required ChartSeriesDefinition ChartSeries { get; set; }
    public required DataSet SourceDataSet { get; set; }
    public DataSet? PreProcessedDataSet { get; set; }
    public DataSet? ProcessedDataSet { get; set; }
}
