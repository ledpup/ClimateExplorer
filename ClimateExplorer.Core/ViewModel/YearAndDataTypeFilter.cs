using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.ViewModel;

public class YearAndDataTypeFilter
{
    public YearAndDataTypeFilter (short year)
    {
        Year = year;
    }

    public short Year { get; set; }
    public DataType? DataType { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
}
