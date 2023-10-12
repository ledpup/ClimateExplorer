using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

namespace ClimateExplorer.Visualiser.UiModel;

public class SourceSeriesSpecification
{
    public DataSetDefinitionViewModel? DataSetDefinition { get; set; }
    public MeasurementDefinitionViewModel? MeasurementDefinition { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }

    public static SourceSeriesSpecification[] BuildArray(LocationBase? location, DataSetAndMeasurementDefinition dsdmd)
    {
        if (dsdmd == null) return new SourceSeriesSpecification[0];

        return
            new SourceSeriesSpecification[]
            {
                    new SourceSeriesSpecification
                    {
                        LocationId = location == null ? null : location.Id,
                        LocationName = location == null ? null : location.Name,
                        DataSetDefinition = dsdmd.DataSetDefinition!,
                        MeasurementDefinition = dsdmd.MeasurementDefinition!,
                    }
            };
    }
}
