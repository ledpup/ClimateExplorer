using ClimateExplorer.Core.Model;

namespace ClimateExplorer.Visualiser.UiModel;

public class SeriesWithData
{
    public ChartSeriesDefinition ChartSeries { get; set; }
    public DataSet SourceDataSet { get; set; }
    public DataSet PreProcessedDataSet { get; set; }
    public DataSet ProcessedDataSet { get; set; }
}
