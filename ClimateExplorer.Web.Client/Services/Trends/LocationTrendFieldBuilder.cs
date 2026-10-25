namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Chooses which of a location's pre-calculated temperature trends the dashboard shows. The last 30
/// years wins when it's significant, since it says the most about the climate now; otherwise the full
/// record when that's significant; otherwise nothing. The tooltip also mentions the other trend, but
/// only when it is significant too.
/// </summary>
public static class LocationTrendFieldBuilder
{
    // The location model's trends are fitted to yearly mean temperatures, in °C. Slopes are differences.
    private const UnitOfMeasure Unit = UnitOfMeasure.DegreesCelsius;
    private const string ClickHint = "<p style=\"margin: 12px 0 0 0;\">Click for information on trends.</p>";

    /// <param name="location">The location whose pre-calculated trends to choose from.</param>
    /// <param name="units">The reader's unit system. Defaults to metric.</param>
    public static LocationTrendField? Build(Location location, IUnitFormatter? units = null)
    {
        ArgumentNullException.ThrowIfNull(location);

        units ??= UnitFormatter.Metric;

        if (location.LastYear is not { } lastYear)
        {
            return null;
        }

        var full = location.FullPeriodTemperatureTrend;
        var last30 = location.Last30YearTemperatureTrend;

        var tooltip = "<p>Statistically significant linear trends.</p>";

        if (last30 is { IsSignificant: true })
        {
            var rows = BuildRow($"Last {TrendDefaults.RecentWindowYears} years", last30, lastYear, units);

            if (full is { IsSignificant: true })
            {
                rows += BuildRow("Full period", full, lastYear, units);
            }

            return new LocationTrendField($"Last {TrendDefaults.RecentWindowYears} years trend", FormatRate(last30, units), tooltip + WrapRows(rows) + ClickHint, last30.Slope);
        }

        if (full is { IsSignificant: true })
        {
            return new LocationTrendField("Trend", FormatRate(full, units), tooltip + WrapRows(BuildRow("Full period", full, lastYear, units)) + ClickHint, full.Slope);
        }

        return null;
    }

    // The per-decade rate without the "/decade" suffix, for a column that labels the period itself.
    public static string FormatAmountPerDecade(double slopePerYear, IUnitFormatter? units = null) =>
        TrendFormatting.FormatPerDecadeAmount(slopePerYear, Unit, units ?? UnitFormatter.Metric);

    // The tooltip is rendered outside any component's scoped CSS, so the layout is inline. A headerless
    // table keeps the period labels, year ranges and rates aligned in columns when there are two rows.
    private static string WrapRows(string rows) => $"<table style=\"border-collapse: collapse; margin: 0 auto;\">{rows}</table>";

    private static string BuildRow(string label, TrendSummary trend, int lastYear, IUnitFormatter units) =>
        "<tr>"
        + $"<td style=\"text-align: left; padding-right: 12px; white-space: nowrap;\">{label}</td>"
        + $"<td style=\"text-align: left; padding-right: 12px; white-space: nowrap;\">{DescribePeriod(trend, lastYear)}</td>"
        + $"<td style=\"text-align: right; white-space: nowrap;\">{FormatRate(trend, units)}</td>"
        + "</tr>";

    private static string DescribePeriod(TrendSummary trend, int lastYear) => $"{trend.FirstYear}–{lastYear}";

    private static string FormatRate(TrendSummary trend, IUnitFormatter units) => TrendFormatting.FormatPerDecadeValue(trend.Slope, Unit, units);
}
