namespace ClimateExplorer.Web.UiModel;

public class YearAndValue
{
    public short Year { get; set; }
    public double Value { get; set; }

    public YearAndValue(short year, double value)
    {
        Year = year;
        Value = value;
    }
}
