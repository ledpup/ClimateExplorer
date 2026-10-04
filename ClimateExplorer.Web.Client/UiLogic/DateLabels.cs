namespace ClimateExplorer.Web.UiLogic;

using System.Globalization;
using Microsoft.JSInterop;

/// <summary>
/// Dates as display text, with the day and month in the order the reader's browser uses
/// ("16 June" or "June 16"). The short forms use the three-letter month, for where space is tight.
/// </summary>
/// <remarks>
/// The web projects run with InvariantGlobalization, so <see cref="CultureInfo.CurrentCulture"/>
/// never reflects the browser and can't be used to pick the order. <see cref="MonthFirst"/> is
/// instead set once, at WebAssembly start-up, from the browser's own date formatting. Anything
/// rendered before that (or on the server) uses day-first.
/// </remarks>
public static class DateLabels
{
    public static bool MonthFirst { get; set; }

    public static string DayMonth(DateOnly date)
    {
        return DayMonth(date.Day, date.Month);
    }

    public static string DayMonth(int day, int month)
    {
        return Order(day, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month));
    }

    public static string DayMonthYear(DateOnly date)
    {
        return WithYear(DayMonth(date), date.Year);
    }

    public static string ShortDayMonth(DateOnly date)
    {
        return ShortDayMonth(date.Day, date.Month);
    }

    public static string ShortDayMonth(int day, int month)
    {
        return Order(day, CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month));
    }

    public static string ShortDayMonthYear(DateOnly date)
    {
        return WithYear(ShortDayMonth(date), date.Year);
    }

    /// <summary>
    /// Asks the browser to format a day and month in its own locale and records which came first.
    /// </summary>
    public static async Task DetectBrowserOrderAsync(IJSRuntime jsRuntime)
    {
        try
        {
            await using var formatter = await jsRuntime.InvokeConstructorAsync(
                "Intl.DateTimeFormat",
                Array.Empty<string>(),
                new { month = "long", day = "numeric" });

            var parts = await formatter.InvokeAsync<DatePart[]>("formatToParts");

            MonthFirst = IsMonthFirst(parts.Select(x => x.Type));
        }
        catch (Exception)
        {
            // Date order is a nicety - never let it stop the app from starting
            MonthFirst = false;
        }
    }

    public static bool IsMonthFirst(IEnumerable<string?> partTypes)
    {
        return partTypes.FirstOrDefault(x => x is "month" or "day") == "month";
    }

    private static string Order(int day, string monthName)
    {
        return MonthFirst ? $"{monthName} {day}" : $"{day} {monthName}";
    }

    private static string WithYear(string dayMonth, int year)
    {
        return MonthFirst ? $"{dayMonth}, {year}" : $"{dayMonth} {year}";
    }

    private sealed record DatePart(string? Type, string? Value);
}
