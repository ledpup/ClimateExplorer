namespace ClimateExplorer.Core.ViewModel;

using static ClimateExplorer.Core.Enums;

public class MeasurementDefinitionViewModel
{
    public DataType DataType { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public DataResolution DataResolution { get; set; }
}
