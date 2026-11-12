namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;

/// <summary>
/// The one place the trend windows are named, so the chart's dropdown, the About-trends panels and
/// the notifications all call them the same thing.
/// </summary>
public static class TrendWindowLabel
{
    public static string Get(TrendWindow window)
    {
        return window switch
        {
            TrendWindow.Full => "Full period",
            TrendWindow.Last30 => $"Last {ChartSeriesTrendCalculator.RecentWindowYears} years",
            TrendWindow.RecentDecade => $"Last {ChartSeriesTrendCalculator.RecentDecadeWindowYears} years",
            TrendWindow.FirstHalf => "Early period",
            TrendWindow.Since1961 => $"Since {TrendDefaults.FixedPeriodStartYear}",
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
            TrendWindow.Last30 => $"the last {ChartSeriesTrendCalculator.RecentWindowYears} years",
            TrendWindow.RecentDecade => $"the last {ChartSeriesTrendCalculator.RecentDecadeWindowYears} years",
            TrendWindow.FirstHalf => "the early period (the first half of the record)",
            TrendWindow.Since1961 => $"since {TrendDefaults.FixedPeriodStartYear}, the start of the WMO reference period",
            _ => throw new NotImplementedException($"TrendWindow {window}"),
        };
    }
}
