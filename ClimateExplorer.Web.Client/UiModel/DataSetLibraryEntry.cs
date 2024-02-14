namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.DataPreparation;
using static ClimateExplorer.Core.Enums;

public class DataSetLibraryEntry
{
    public string? Name { get; set; }
    public SeriesDerivationTypes SeriesDerivationType { get; set; }
    public SeriesAggregationOptions SeriesAggregation { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public SourceSeriesSpecification[]? SourceSeriesSpecifications { get; set; }

    public class SourceSeriesSpecification
    {
        required public Guid SourceDataSetId { get; set; }
        required public DataType DataType { get; set; }
        required public Guid LocationId { get; set; }
        public DataAdjustment? DataAdjustment { get; set; }
        public string? LocationName { get; set; }
    }
}
