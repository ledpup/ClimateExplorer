namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.DataPreparation;

public class DataDownloadPackage
{
    public List<SeriesWithData>? ChartSeriesWithData { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public BinIdentifier[]? Bins { get; set; }
    public BinGranularities BinGranularity { get; set; }
}
