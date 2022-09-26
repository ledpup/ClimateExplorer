using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.ViewModel;

public class MeasurementDefinitionViewModel
{
    public DataCategory? DataCategory { get; set; }
    public DataType DataType { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
}
