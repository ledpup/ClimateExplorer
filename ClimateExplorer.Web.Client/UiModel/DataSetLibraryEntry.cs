using ClimateExplorer.Core.DataPreparation;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Web.UiModel;

public class DataSetLibraryEntry
{
    public string? Name { get; set; }
    public SeriesDerivationTypes SeriesDerivationType { get; set; }
    public SeriesAggregationOptions SeriesAggregation { get; set; }
    public SourceSeriesSpecification[]? SourceSeriesSpecifications { get; set; }

    public class SourceSeriesSpecification
    {
        public required Guid SourceDataSetId { get; set; }
        public required DataType DataType { get; set; }
        public required Guid LocationId { get; set; }
        public DataAdjustment? DataAdjustment { get; set; }
        public string? LocationName { get; set; }
    }
}
