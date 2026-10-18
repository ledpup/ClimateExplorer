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

        var tooltip = "<p>Statistically significant linear trends.</p>";

        if (last30 is { IsSignificant: true })
        {
            var rows = BuildRow($"Last {TrendDefaults.RecentWindowYears} years", last30, lastYear);

            if (full is { IsSignificant: true })
            {
                rows += BuildRow("Full period", full, lastYear);
            }

            return new LocationTrendField($"Last {TrendDefaults.RecentWindowYears} year trend", FormatRate(last30), tooltip + WrapRows(rows));
        }

        if (full is { IsSignificant: true })
        {
            return new LocationTrendField("Trend", FormatRate(full), tooltip + WrapRows(BuildRow("Full period", full, lastYear)));
        }

        return null;
    }

    // The tooltip is rendered outside any component's scoped CSS, so the layout is inline. A headerless
    // table keeps the period labels, year ranges and rates aligned in columns when there are two rows.
    private static string WrapRows(string rows) => $"<table style=\"border-collapse: collapse; margin: 0 auto;\">{rows}</table>";

    private static string BuildRow(string label, TrendSummary trend, int lastYear) =>
        "<tr>"
        + $"<td style=\"text-align: left; padding-right: 12px; white-space: nowrap;\">{label}</td>"
        + $"<td style=\"text-align: left; padding-right: 12px; white-space: nowrap;\">{DescribePeriod(trend, lastYear)}</td>"
        + $"<td style=\"text-align: right; white-space: nowrap;\">{FormatRate(trend)}</td>"
        + "</tr>";

    private static string DescribePeriod(TrendSummary trend, int lastYear) => $"{trend.FirstYear}–{lastYear}";

    private static string FormatRate(TrendSummary trend) => TrendFormatting.FormatPerDecadeValue(trend.Slope, Unit);
}
