namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// Chooses which of a location's pre-calculated temperature trends the dashboard shows. The last 30
/// years wins when it's significant, since it says the most about the climate now; otherwise the full
/// record when that's significant; otherwise nothing. The tooltip also mentions the other trend, but
/// only when it is significant too.
/// </summary>
public static class LocationTrendFieldBuilder
{
    private const string Unit = "°C";

    public static LocationTrendField? Build(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (location.LastYear is not { } lastYear)
        {
            return null;
        }

        var full = location.FullPeriodTemperatureTrend;
        var last30 = location.Last30YearTemperatureTrend;

        if (last30 is { IsSignificant: true })
        {
            var tooltip = $"Statistically significant linear trend over the last {TrendDefaults.RecentWindowYears} years of records "
                + $"({DescribePeriod(last30, lastYear)}): {FormatRate(last30)}.";

            if (full is { IsSignificant: true })
            {
                tooltip += $"<br>Full record ({DescribePeriod(full, lastYear)}): {FormatRate(full)}.";
            }

            return new LocationTrendField($"Last {TrendDefaults.RecentWindowYears} year trend", FormatRate(last30), tooltip);
        }

        if (full is { IsSignificant: true })
        {
            return new LocationTrendField(
                "Trend",
                FormatRate(full),
                $"Statistically significant linear trend over the full record ({DescribePeriod(full, lastYear)}): {FormatRate(full)}.");
        }

        return null;
    }

    private static string DescribePeriod(TrendSummary trend, int lastYear) => $"{trend.FirstYear}–{lastYear}";

    private static string FormatRate(TrendSummary trend) => TrendFormatting.FormatPerDecadeValue(trend.Slope, Unit);
}
