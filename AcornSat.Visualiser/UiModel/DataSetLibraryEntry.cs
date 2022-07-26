using ClimateExplorer.Core.DataPreparation;
using static AcornSat.Core.Enums;

namespace AcornSat.Visualiser.UiModel
{
    public class DataSetLibraryEntry
    {
        public string Name { get; set; }
        public SeriesDerivationTypes SeriesDerivationType { get; set; }
        public SeriesAggregationOptions SeriesAggregation { get; set; }
        public SourceSeriesSpecification[] SourceSeriesSpecifications { get; set; }

        public class SourceSeriesSpecification
        {
            public Guid SourceDataSetId { get; set; }
            public DataType DataType { get; set; }
            public DataAdjustment? DataAdjustment { get; set; }
            public Guid? LocationId { get; set; }
            public string? LocationName { get; set; }
        }
    }
}
