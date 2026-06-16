namespace ClimateExplorer.Core.Calculators;

using System.Globalization;

public static class MeteorologicalSeasonCalculator
{
    public static MeteorologicalHemisphere GetHemisphere(double latitude)
    {
        return latitude < 0d
            ? MeteorologicalHemisphere.Southern
            : MeteorologicalHemisphere.Northern;
    }

    public static DateOnly GetCurrentSeasonStartDate(DateOnly date)
    {
        if (date.Month <= 2)
        {
            return new DateOnly(date.Year - 1, 12, 1);
        }

        if (date.Month <= 5)
        {
            return new DateOnly(date.Year, 3, 1);
        }

        if (date.Month <= 8)
        {
            return new DateOnly(date.Year, 6, 1);
        }

        if (date.Month <= 11)
        {
            return new DateOnly(date.Year, 9, 1);
        }

        return new DateOnly(date.Year, 12, 1);
    }

    public static MeteorologicalSeasonPeriod GetCurrentSeason(DateOnly date, double latitude)
    {
        return CreateSeasonPeriod(GetCurrentSeasonStartDate(date), GetHemisphere(latitude));
    }

    public static MeteorologicalSeasonPeriod GetCurrentSeasonToDate(DateOnly date, double latitude)
    {
        return GetCurrentSeason(date, latitude) with { EndDate = date };
    }

    public static bool IsCurrentSeasonToDateMeaningful(DateOnly date)
    {
        return date >= GetCurrentSeasonStartDate(date).AddMonths(1);
    }

    public static IReadOnlyList<MeteorologicalSeasonPeriod> GetPreviousSeasons(DateOnly date, double latitude, int count)
    {
        var periods = new List<MeteorologicalSeasonPeriod>();
        var currentSeasonStart = GetCurrentSeasonStartDate(date);
        var hemisphere = GetHemisphere(latitude);

        for (var offset = 1; offset <= count; offset++)
        {
            periods.Add(CreateSeasonPeriod(currentSeasonStart.AddMonths(-3 * offset), hemisphere));
        }

        return periods;
    }

    public static string FormatTitle(MeteorologicalSeasonPeriod period, bool toDate)
    {
        return toDate
            ? $"{period.Season} to Date"
            : $"{period.Season} {FormatSeasonYear(period)}";
    }

    public static string FormatComparisonLabel(MeteorologicalSeasonPeriod period, bool toDate)
    {
        return toDate
            ? $"{period.Season} to date"
            : period.Season.ToString();
    }

    public static string FormatComparisonLabelPlural(MeteorologicalSeasonPeriod period, bool toDate)
    {
        return toDate
            ? $"{period.Season}-to-date periods"
            : $"{period.Season}s";
    }

    public static string FormatSeasonYear(MeteorologicalSeasonPeriod period)
    {
        if (!period.SpansCalendarYears)
        {
            return period.StartDate.Year.ToString(CultureInfo.InvariantCulture);
        }

        return $"{period.StartDate.Year}-{(period.EndDate.Year % 100).ToString("00", CultureInfo.InvariantCulture)}";
    }

    private static MeteorologicalSeasonPeriod CreateSeasonPeriod(DateOnly startDate, MeteorologicalHemisphere hemisphere)
    {
        return new MeteorologicalSeasonPeriod(
            GetSeasonForStartMonth(startDate.Month, hemisphere),
            hemisphere,
            startDate,
            startDate.AddMonths(3).AddDays(-1));
    }

    private static MeteorologicalSeason GetSeasonForStartMonth(int startMonth, MeteorologicalHemisphere hemisphere)
    {
        return (startMonth, hemisphere) switch
        {
            (12, MeteorologicalHemisphere.Northern) => MeteorologicalSeason.Winter,
            (12, MeteorologicalHemisphere.Southern) => MeteorologicalSeason.Summer,
            (3, MeteorologicalHemisphere.Northern) => MeteorologicalSeason.Spring,
            (3, MeteorologicalHemisphere.Southern) => MeteorologicalSeason.Autumn,
            (6, MeteorologicalHemisphere.Northern) => MeteorologicalSeason.Summer,
            (6, MeteorologicalHemisphere.Southern) => MeteorologicalSeason.Winter,
            (9, MeteorologicalHemisphere.Northern) => MeteorologicalSeason.Autumn,
            (9, MeteorologicalHemisphere.Southern) => MeteorologicalSeason.Spring,
            _ => throw new ArgumentOutOfRangeException(nameof(startMonth), startMonth, "Season start month must be 3, 6, 9, or 12."),
        };
    }
}
