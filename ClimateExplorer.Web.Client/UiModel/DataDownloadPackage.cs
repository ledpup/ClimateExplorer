namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Client.Units;

public sealed record DataDownloadPackage
{
    public List<SeriesWithData>? ChartSeriesWithData { get; set; }
    public BinIdentifier[]? Bins { get; set; }
    public BinGranularities BinGranularity { get; set; }

    /// <summary>The unit system the chart was drawn in, which the export uses too.</summary>
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;
}
