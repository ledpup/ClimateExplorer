using static AcornSat.Core.Enums;

namespace AcornSat.Core.ViewModel;

public class MeasurementDefinitionViewModel
{
    public DataCategory? DataCategory { get; set; }
    public DataType DataType { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public int PreferredColour { get; set; }
}
