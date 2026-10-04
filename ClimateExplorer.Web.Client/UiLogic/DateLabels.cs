namespace ClimateExplorer.Web.UiLogic;

using System.Globalization;

/// <summary>
/// Dates as display text, with the day and month in the order the reader's browser uses
/// ("16 June" or "June 16"). The short forms use the three-letter month, for where space is tight.
/// </summary>
/// <remarks>
/// The web projects run with InvariantGlobalization, so <see cref="CultureInfo.CurrentCulture"/>
/// never reflects the browser and can't be used to pick the order. Instead this is a per-user
/// (scoped) service: the server sets <see cref="MonthFirst"/> from the request's Accept-Language
/// header, and <c>Routes</c> hands that same answer on to the interactive server circuit and to
/// WebAssembly, so a date never changes order under the reader.
/// </remarks>
public sealed class DateLabels
{
    // Regions that write "June 16" in English. Everywhere else English is day-first.
    private static readonly HashSet<string> MonthFirstEnglishRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "CA", "AS", "GU", "MH", "MP", "PR", "UM", "VI",
    };

    // Languages that put the month before the day whatever the region
    private static readonly HashSet<string> MonthFirstLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ja", "zh", "ko", "hu", "lt", "mn", "eu", "fil",
    };

    public bool MonthFirst { get; set; }

    /// <summary>
    /// Whether a browser language tag (e.g. "en-US", from Accept-Language or navigator.language)
    /// belongs to a locale that writes the month before the day.
    /// </summary>
    public static bool IsMonthFirstLocale(string? languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            return false;
        }

        var subtags = languageTag.Trim().Split('-', '_');
        var language = subtags[0];

        if (!language.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return MonthFirstLanguages.Contains(language);
        }

        // A bare "en" is treated as US English, as browsers do. Otherwise the region decides -
        // it's the two-letter subtag, which may follow a script ("en-Latn-US").
        var region = subtags.Skip(1).FirstOrDefault(x => x.Length == 2);

        return region is null || MonthFirstEnglishRegions.Contains(region);
    }

    /// <summary>
    /// As <see cref="IsMonthFirstLocale"/>, for a whole Accept-Language header
    /// (e.g. "en-US,en;q=0.9,fr;q=0.8"): the reader's most preferred language decides.
    /// </summary>
    public static bool IsMonthFirstAcceptLanguage(string? acceptLanguage)
    {
        string? preferred = null;
        var preferredQuality = 0.0;

        foreach (var entry in (acceptLanguage ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(';', StringSplitOptions.TrimEntries);
            var quality = 1.0;

            if (parts.Length > 1
                && parts[1].StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                && !double.TryParse(parts[1][2..], NumberStyles.Float, CultureInfo.InvariantCulture, out quality))
            {
                continue;
            }

            // Earlier entries win ties, and "*" says nothing about the reader's locale
            if (parts[0] != "*" && quality > preferredQuality)
            {
                preferred = parts[0];
                preferredQuality = quality;
            }
        }

        return IsMonthFirstLocale(preferred);
    }

    public string DayMonth(DateOnly date)
    {
        return DayMonth(date.Day, date.Month);
    }

    public string DayMonth(int day, int month)
    {
        return Order(day, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month));
    }

    public string DayMonthYear(DateOnly date)
    {
        return WithYear(DayMonth(date), date.Year);
    }

    public string ShortDayMonth(DateOnly date)
    {
        return ShortDayMonth(date.Day, date.Month);
    }

    public string ShortDayMonth(int day, int month)
    {
        return Order(day, CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month));
    }

    public string ShortDayMonthYear(DateOnly date)
    {
        return WithYear(ShortDayMonth(date), date.Year);
    }

    private string Order(int day, string monthName)
    {
        return MonthFirst ? $"{monthName} {day}" : $"{day} {monthName}";
    }

    private string WithYear(string dayMonth, int year)
    {
        return MonthFirst ? $"{dayMonth}, {year}" : $"{dayMonth} {year}";
    }
}
