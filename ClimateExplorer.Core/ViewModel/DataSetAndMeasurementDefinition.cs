namespace ClimateExplorer.Core.ViewModel;

public sealed record DataSetAndMeasurementDefinition
{
    public DataSetDefinitionViewModel? DataSetDefinition { get; set; }
    public MeasurementDefinitionViewModel? MeasurementDefinition { get; set; }
}
