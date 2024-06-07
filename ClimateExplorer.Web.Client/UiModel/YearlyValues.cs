namespace ClimateExplorer.Web.UiModel;

public class YearlyValues(short year, double relative, double absolute)
{
    public short Year { get; set; } = year;
    public double Relative { get; set; } = relative;
    public double Absolute { get; set; } = absolute;
}
