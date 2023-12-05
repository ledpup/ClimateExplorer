using ClimateExplorer.Core.DataPreparation;

namespace ClimateExplorer.Web.UiModel;

public class DataDownloadPackage
{
    public List<SeriesWithData>? ChartSeriesWithData { get; set; }
    public BinIdentifier[]? Bins { get; set; }
    public BinGranularities BinGranularity { get; set; }
}
