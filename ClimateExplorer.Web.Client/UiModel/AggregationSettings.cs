namespace ClimateExplorer.Web.Client.UiModel;

public record AggregationSettings(short GroupingDays, string ThresholdText, bool UserOverride)
{
    public static string GroupingDaysText(int groupingDays) =>
        groupingDays switch
        {
            7 => "Weekly",
            14 => "Fortnightly",
            28 => "Monthly (28 days)",
            91 => "Quarterly",
            182 => "Half-yearly",
            _ => throw new NotImplementedException(groupingDays.ToString()),
        };
}