namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Web.UiLogic;

// Shared string/number formatting used across the other RecentObservationsCalculator.*.cs
// files: value formatting per unit, date/month formatting, and small text helpers.
public sealed partial class RecentObservationsCalculator
{
    private static string FormatTemperature(double value)
    {
        return $"{value.ToString("0.0", CultureInfo.InvariantCulture)}°C";
    }

    private static string FormatPrecipitation(double value)
    {
        return $"{value.ToString("0", CultureInfo.InvariantCulture)}mm";
    }

    private static string FormatCo2(double value)
    {
        return $"{value.ToString("0.0", CultureInfo.InvariantCulture)} ppm";
    }

    private static string FormatAnomaly(double value, Metric metric)
    {
        return $"{(value >= 0 ? "+" : string.Empty)}{metric.Format(value)}";
    }

    private static string FormatStandardScore(double value)
    {
        return $"{(value >= 0 ? "+" : string.Empty)}{value.ToString("0.0", CultureInfo.InvariantCulture)}×";
    }

    private static string? FormatHistoricalOccurrence(HistoricalPeriodValue? value)
    {
        return value?.Year?.ToString(CultureInfo.InvariantCulture);
    }

    private static string Pluralize(string singular, int count)
    {
        return count == 1 ? singular : $"{singular}s";
    }

    private static string LowerFirst(string value)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : value[..1].ToLower(CultureInfo.InvariantCulture) + value[1..];
    }

    private static string MonthName(int month)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
    }

    private string FormatFullDate(DateOnly date)
    {
        return dateLabels.DayMonthYear(date);
    }

    private string FormatDayMonth(DateOnly date)
    {
        return dateLabels.DayMonth(date);
    }

    private string FormatShortDayMonth(DateOnly date)
    {
        return dateLabels.ShortDayMonth(date);
    }

    private string FormatDayMonthYear(DateOnly date)
    {
        return dateLabels.ShortDayMonthYear(date);
    }
}
