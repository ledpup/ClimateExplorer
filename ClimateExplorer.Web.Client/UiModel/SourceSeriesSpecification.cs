using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

namespace ClimateExplorer.Web.UiModel;

public class SourceSeriesSpecification
{
    public DataSetDefinitionViewModel? DataSetDefinition { get; set; }
    public MeasurementDefinitionViewModel? MeasurementDefinition { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }

    public static SourceSeriesSpecification[] BuildArray(LocationBase? location, DataSetAndMeasurementDefinition dsdmd)
    {
        if (dsdmd == null) return [];

        return
            [
                new SourceSeriesSpecification
                {
                    LocationId = location?.Id,
                    LocationName = location?.Name,
                    DataSetDefinition = dsdmd.DataSetDefinition!,
                    MeasurementDefinition = dsdmd.MeasurementDefinition!,
                }
            ];
    }
}
