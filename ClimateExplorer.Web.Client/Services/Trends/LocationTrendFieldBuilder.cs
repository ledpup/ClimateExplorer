namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Chooses which of a location's pre-calculated temperature trends the dashboard shows: the first
/// significant one of the last 30 years (it says the most about the climate now), since 1961 (the same
/// period at every location) and the full record; otherwise nothing. The tooltip lists every significant
/// trend, in that order.
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

        TrendSummary?[] candidates = [location.Last30YearTemperatureTrend, location.Since1961TemperatureTrend, location.FullPeriodTemperatureTrend];
        var significant = candidates.OfType<TrendSummary>().Where(x => x.IsSignificant).ToList();

        if (significant.Count == 0)
        {
            return null;
        }

        var shown = significant[0];
        var rows = string.Concat(significant.Select(x => BuildRow(x, lastYear, units)));
        var tooltip = "<p>Statistically significant linear trends.</p>" + WrapRows(rows) + ClickHint;

        return new LocationTrendField(FieldLabel(shown.Window), FormatRate(shown, units), tooltip, shown.Slope);
    }

    // The per-decade rate without the "/decade" suffix, for a column that labels the period itself.
    public static string FormatAmountPerDecade(double slopePerYear, IUnitFormatter? units = null) =>
        TrendFormatting.FormatPerDecadeAmount(slopePerYear, Unit, units ?? UnitFormatter.Metric);

    // The tooltip is rendered outside any component's scoped CSS, so the layout is inline. A headerless
    // table keeps the period labels, year ranges and rates aligned in columns when there are several rows.
    private static string WrapRows(string rows) => $"<table style=\"border-collapse: collapse; margin: 0 auto;\">{rows}</table>";

    private static string FieldLabel(TrendWindow window) => window switch
    {
        TrendWindow.Last30 => $"Last {TrendDefaults.RecentWindowYears} years trend",
        TrendWindow.Since1961 => $"Trend since {TrendDefaults.FixedPeriodStartYear}",
        _ => "Trend",
    };

    private static string BuildRow(TrendSummary trend, int lastYear, IUnitFormatter units) =>
        "<tr>"
        + $"<td style=\"text-align: left; padding-right: 12px; white-space: nowrap;\">{TrendWindowLabel.Get(trend.Window)}</td>"
        + $"<td style=\"text-align: left; padding-right: 12px; white-space: nowrap;\">{DescribePeriod(trend, lastYear)}</td>"
        + $"<td style=\"text-align: right; white-space: nowrap;\">{FormatRate(trend, units)}</td>"
        + "</tr>";

    private static string DescribePeriod(TrendSummary trend, int lastYear) => $"{trend.FirstYear}–{lastYear}";

    private static string FormatRate(TrendSummary trend, IUnitFormatter units) => TrendFormatting.FormatPerDecadeValue(trend.Slope, Unit, units);
}
