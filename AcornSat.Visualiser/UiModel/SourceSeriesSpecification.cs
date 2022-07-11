using AcornSat.Core.ViewModel;
using ClimateExplorer.Core.ViewModel;

namespace AcornSat.Visualiser.UiModel
{
    public class SourceSeriesSpecification
    {
        public DataSetDefinitionViewModel DataSetDefinition { get; set; }
        public MeasurementDefinitionViewModel MeasurementDefinition { get; set; }
        public Guid? LocationId { get; set; }
        public string? LocationName { get; set; }

        public static SourceSeriesSpecification[] BuildArray(Location location, DataSetAndMeasurementDefinition dsdmd)
        {
            if (dsdmd == null) return new SourceSeriesSpecification[0];

            return
                new SourceSeriesSpecification[]
                {
                        new SourceSeriesSpecification
                        {
                            LocationId = location.Id,
                            LocationName = location.Name,
                            DataSetDefinition = dsdmd.DataSetDefinition,
                            MeasurementDefinition = dsdmd.MeasurementDefinition,
                        }
                };
        }
    }
}
