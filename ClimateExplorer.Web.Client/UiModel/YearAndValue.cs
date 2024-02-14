namespace ClimateExplorer.Web.UiModel;

public class YearAndValue
{
    public YearAndValue(short year, double value)
    {
        Year = year;
        Value = value;
    }

    public short Year { get; set; }
    public double Value { get; set; }
}
