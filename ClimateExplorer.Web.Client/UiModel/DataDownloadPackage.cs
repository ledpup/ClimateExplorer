namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.DataPreparation;

public class DataDownloadPackage
{
    public List<SeriesWithData>? ChartSeriesWithData { get; set; }
    public BinIdentifier[]? Bins { get; set; }
    public BinGranularities BinGranularity { get; set; }
}
