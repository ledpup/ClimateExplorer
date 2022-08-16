namespace ClimateExplorer.Visualiser.UiModel;

public class YearAndValue
{
    public int Year { get; set; }
    public float Value { get; set; }

    public YearAndValue(int year, float value)
    {
        Year = year;
        Value = value;
    }
}
