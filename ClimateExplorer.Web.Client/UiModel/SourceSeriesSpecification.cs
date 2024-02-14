namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

public class SourceSeriesSpecification
{
    public Guid SourceDataSetId { get; set; }
    required public Guid LocationId { get; set; }
    required public string LocationName { get; set; }
    public DataSetDefinitionViewModel? DataSetDefinition { get; set; }
    public MeasurementDefinitionViewModel? MeasurementDefinition { get; set; }

    public static SourceSeriesSpecification[] BuildArray(GeographicalEntity location, DataSetAndMeasurementDefinition dsdmd)
    {
        if (dsdmd == null)
        {
            return [];
        }

        return
            [
                new SourceSeriesSpecification
                {
                    LocationId = location.Id,
                    LocationName = location.Name,
                    DataSetDefinition = dsdmd.DataSetDefinition!,
                    MeasurementDefinition = dsdmd.MeasurementDefinition!,
                }

            ];
    }
}
