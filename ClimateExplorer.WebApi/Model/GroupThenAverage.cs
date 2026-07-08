namespace ClimateExplorer.WebApi.Model;

public class GroupThenAverage(short dayGrouping = 14, float dayGroupingThreshold = .7f) : StatsParameters
{
    public short DayGrouping { get; set; } = dayGrouping;
    public float DayGroupingThreshold { get; set; } = dayGroupingThreshold;
}
