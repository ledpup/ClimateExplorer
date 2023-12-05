namespace ClimateExplorer.Web.UiModel;

public class YearAndValue
{
    public short Year { get; set; }
    public float Value { get; set; }

    public YearAndValue(short year, float value)
    {
        Year = year;
        Value = value;
    }
}
