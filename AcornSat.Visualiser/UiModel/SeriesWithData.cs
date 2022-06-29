using AcornSat.Core.Model;

namespace AcornSat.Visualiser.UiModel;

public class SeriesWithData
{
    public ChartSeriesDefinition ChartSeries { get; set; }
    public List<DataSet> SourceDataSets { get; set; }
    public List<DataSet> PreProcessedDataSets { get; set; }
    public List<DataSet> ProcessedDataSets { get; set; }
}
