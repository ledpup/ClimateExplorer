namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// The one place the three trend windows are named, so the chart's dropdown, the About-trends
/// panels and the notifications all call them the same thing.
/// </summary>
public static class TrendWindowLabel
{
    public static string Get(TrendWindow window)
    {
        return window switch
        {
            TrendWindow.Full => "Full period",
            TrendWindow.Recent => $"Last {ChartSeriesTrendCalculator.RecentWindowYears} years",
            TrendWindow.FirstHalf => "Early period",
            _ => throw new NotImplementedException($"TrendWindow {window}"),
        };
    }

    /// <summary>
    /// The longer form used where the window needs explaining rather than just naming - for
    /// example in a notification the user may be reading away from the chart.
    /// </summary>
    public static string GetDescription(TrendWindow window)
    {
        return window switch
        {
            TrendWindow.Full => "the full plotted period",
            TrendWindow.Recent => $"the last {ChartSeriesTrendCalculator.RecentWindowYears} years",
            TrendWindow.FirstHalf => "the early period (the first half of the record)",
            _ => throw new NotImplementedException($"TrendWindow {window}"),
        };
    }
}
