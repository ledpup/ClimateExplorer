namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

public class SourceSeriesSpecification
{
    public Guid SourceDataSetId { get; set; }
    required public Guid LocationId { get; set; }
    public DataSetDefinitionViewModel? DataSetDefinition { get; set; }
    public MeasurementDefinitionViewModel? MeasurementDefinition { get; set; }

    public static SourceSeriesSpecification[] BuildArray(Guid locationId, DataSetAndMeasurementDefinition dsdmd)
    {
        if (dsdmd == null)
        {
            return [];
        }

        return
            [
                new SourceSeriesSpecification
                {
                    LocationId = locationId,
                    DataSetDefinition = dsdmd.DataSetDefinition!,
                    MeasurementDefinition = dsdmd.MeasurementDefinition!,
                },
            ];
    }
}
