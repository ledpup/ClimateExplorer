namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

public sealed record SourceSeriesSpecification
{
    public Guid SourceDataSetId { get; set; }
    public required Guid LocationId { get; set; }
    public required string LocationName { get; set; }
    public DataSetDefinitionViewModel? DataSetDefinition { get; set; }
    public MeasurementDefinitionViewModel? MeasurementDefinition { get; set; }
    public MeteorologicalHemisphere? Hemisphere { get; set; }

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
                    Hemisphere = (location as Location)?.Coordinates.Latitude is { } latitude ? MeteorologicalSeasonCalculator.GetHemisphere(latitude) : null,
                }

            ];
    }
}
