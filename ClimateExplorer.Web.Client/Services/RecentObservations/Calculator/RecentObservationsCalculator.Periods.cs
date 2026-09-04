#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.Calculators;

// Turns a domain's merged daily series into the fixed set of tile periods (previous days,
// latest 7 days, current/previous month, current/previous season, year-to-date, previous
// years) and the titles/labels shown for each one.
public sealed partial class RecentObservationsCalculator
{
    private static List<PeriodObservation> BuildPeriods(
        List<DailyObservation> daily,
        DateOnly referenceDate,
        DateOnly today,
        double? latitude,
        MetricDomain domain,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        int previousYearCount,
        bool supportsSeasonTiles)
    {
        var periods = new List<PeriodObservation>();

        foreach (var previousDay in GetPreviousDayPeriods(daily, x => x.Date, referenceDate, today, previousDayCount))
        {
            periods.Add(CreateDailyPeriod(previousDay.Title, previousDay.Record, domain, previousDay.Offset));
        }

        var latestSevenDaysStart = referenceDate.AddDays(-(LatestSevenDaysLength - 1));
        AddRangePeriod(
            periods,
            GetRecordsInRange(daily, latestSevenDaysStart, referenceDate),
            latestSevenDaysStart,
            referenceDate,
            PeriodKind.LatestSevenDays,
            domain);

        var currentMonthToDate = GetCurrentMonthToDatePeriod(referenceDate);
        if (currentMonthToDate is not null)
        {
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, currentMonthToDate.StartDate, currentMonthToDate.EndDate),
                currentMonthToDate.StartDate,
                currentMonthToDate.EndDate,
                PeriodKind.CurrentMonth,
                domain);
        }

        foreach (var previousMonth in GetPreviousMonthPeriods(referenceDate, previousMonthCount))
        {
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, previousMonth.StartDate, previousMonth.EndDate),
                previousMonth.StartDate,
                previousMonth.EndDate,
                PeriodKind.PreviousMonth,
                domain,
                previousMonthOffset: previousMonth.Offset);
        }

        var currentSeasonToDate = supportsSeasonTiles && latitude.HasValue
            ? GetCurrentSeasonToDatePeriod(referenceDate, latitude.Value)
            : null;
        if (currentSeasonToDate is not null)
        {
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, currentSeasonToDate.StartDate, currentSeasonToDate.EndDate),
                currentSeasonToDate.StartDate,
                currentSeasonToDate.EndDate,
                PeriodKind.CurrentSeason,
                domain,
                seasonPeriod: currentSeasonToDate,
                isSeasonToDate: !currentSeasonToDate.IsComplete);
        }

        var previousSeasons = supportsSeasonTiles && latitude.HasValue
            ? MeteorologicalSeasonCalculator.GetPreviousSeasons(referenceDate, latitude.Value, previousSeasonCount)
            : Array.Empty<MeteorologicalSeasonPeriod>();
        for (var index = 0; index < previousSeasons.Count; index++)
        {
            var previousSeason = previousSeasons[index];
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, previousSeason.StartDate, previousSeason.EndDate),
                previousSeason.StartDate,
                previousSeason.EndDate,
                PeriodKind.PreviousSeason,
                domain,
                seasonPeriod: previousSeason,
                periodOffset: index + 1);
        }

        var yearToDate = GetYearToDatePeriod(referenceDate);
        if (yearToDate is not null)
        {
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, yearToDate.StartDate, yearToDate.EndDate),
                yearToDate.StartDate,
                yearToDate.EndDate,
                PeriodKind.YearToDate,
                domain);
        }

        foreach (var previousYear in GetPreviousYearPeriods(referenceDate, previousYearCount))
        {
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, previousYear.StartDate, previousYear.EndDate),
                previousYear.StartDate,
                previousYear.EndDate,
                PeriodKind.PreviousYear,
                domain,
                periodOffset: previousYear.Offset);
        }

        return periods;
    }

    private static CurrentPeriod? GetCurrentMonthToDatePeriod(DateOnly referenceDate)
    {
        return referenceDate.Day == 1
            ? null
            : new CurrentPeriod(new DateOnly(referenceDate.Year, referenceDate.Month, 1), referenceDate);
    }

    private static MeteorologicalSeasonPeriod? GetCurrentSeasonToDatePeriod(DateOnly referenceDate, double latitude)
    {
        if (!MeteorologicalSeasonCalculator.IsCurrentSeasonToDateMeaningful(referenceDate))
        {
            return null;
        }

        return MeteorologicalSeasonCalculator.GetCurrentSeason(referenceDate, latitude) with { EndDate = referenceDate };
    }

    private static CurrentPeriod? GetYearToDatePeriod(DateOnly referenceDate)
    {
        return referenceDate.Month == 1
            ? null
            : new CurrentPeriod(new DateOnly(referenceDate.Year, 1, 1), referenceDate);
    }

    private static PeriodObservation CreateDailyPeriod(string title, DailyObservation record, MetricDomain domain, int periodOffset)
    {
        return new PeriodObservation(
            title,
            FormatDayMonth(record.Date),
            $"{FormatDayMonth(record.Date)} days",
            record.Date,
            record.Date,
            ObservationCompleteness.CompleteDay,
            PeriodKind.Daily,
            PeriodComparisonMode.DailyDate,
            periodOffset,
            null,
            ComputeMetrics([record], domain));
    }

    private static void AddRangePeriod(
        List<PeriodObservation> periods,
        List<DailyObservation> records,
        DateOnly startDate,
        DateOnly endDate,
        PeriodKind kind,
        MetricDomain domain,
        int? previousMonthOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false,
        int? periodOffset = null,
        string? note = null)
    {
        if (records.Count == 0)
        {
            return;
        }

        var expectedDays = GetDayCount(startDate, endDate);
        var availableDays = records.Select(x => x.Date).Distinct().Count();
        var completeness = new ObservationCompleteness(availableDays, expectedDays);

        periods.Add(new PeriodObservation(
            CreatePeriodTitle(kind, startDate, endDate, previousMonthOffset, seasonPeriod, isSeasonToDate),
            CreateComparisonLabel(kind, endDate, seasonPeriod, isSeasonToDate),
            CreateComparisonLabelPlural(kind, endDate, seasonPeriod, isSeasonToDate),
            startDate,
            endDate,
            completeness,
            kind,
            PeriodComparisonMode.DailyRange,
            periodOffset ?? previousMonthOffset,
            note,
            ComputeMetrics(records, domain),
            seasonPeriod));
    }

    private static IEnumerable<PreviousDayPeriod<TRecord>> GetPreviousDayPeriods<TRecord>(
        IEnumerable<TRecord> daily,
        Func<TRecord, DateOnly> getDate,
        DateOnly referenceDate,
        DateOnly today,
        int previousDayCount)
    {
        return daily
            .OrderByDescending(getDate)
            .Take(previousDayCount)
            .Select((record, index) => new PreviousDayPeriod<TRecord>(
                record,
                CreateDailyPeriodTitle(getDate(record), referenceDate, today),
                index + 1));
    }

    private static IEnumerable<PreviousMonthPeriod> GetPreviousMonthPeriods(DateOnly today, int previousMonthCount)
    {
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);

        for (var offset = 1; offset <= previousMonthCount; offset++)
        {
            var startDate = currentMonthStart.AddMonths(-offset);
            var endDate = new DateOnly(startDate.Year, startDate.Month, DateTime.DaysInMonth(startDate.Year, startDate.Month));

            yield return new PreviousMonthPeriod(startDate, endDate, offset);
        }
    }

    private static IEnumerable<PreviousYearPeriod> GetPreviousYearPeriods(DateOnly referenceDate, int previousYearCount)
    {
        for (var offset = 1; offset <= previousYearCount; offset++)
        {
            var year = referenceDate.Year - offset;
            yield return new PreviousYearPeriod(
                new DateOnly(year, 1, 1),
                new DateOnly(year, 12, 31),
                offset);
        }
    }

    private static int GetDayCount(DateOnly startDate, DateOnly endDate)
    {
        return endDate.DayNumber - startDate.DayNumber + 1;
    }

    private static string CreatePeriodTitle(
        PeriodKind kind,
        DateOnly startDate,
        DateOnly endDate,
        int? previousMonthOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false)
    {
        if (seasonPeriod is not null)
        {
            return MeteorologicalSeasonCalculator.FormatTitle(seasonPeriod, isSeasonToDate);
        }

        return kind switch
        {
            PeriodKind.LatestSevenDays => "Latest 7 days",
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? $"{MonthName(endDate.Month)} {endDate.Year}"
                : $"{MonthName(endDate.Month)} {endDate.Year} to date",
            PeriodKind.PreviousMonth when previousMonthOffset == 1 => $"Last month - {MonthName(startDate.Month)} {startDate.Year}",
            PeriodKind.PreviousMonth => $"{MonthName(startDate.Month)} {startDate.Year}",
            PeriodKind.YearToDate => IsCalendarYearEnd(endDate)
                ? endDate.Year.ToString(CultureInfo.InvariantCulture)
                : $"{endDate.Year} to date",
            PeriodKind.PreviousYear => startDate.Year.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty,
        };
    }

    private static string CreateHistoricalContextLabel(PeriodObservation period)
    {
        if (period.Kind is PeriodKind.CurrentSeason or PeriodKind.PreviousSeason)
        {
            return period.ComparisonLabel;
        }

        if (period.ComparisonMode == PeriodComparisonMode.DailyDate)
        {
            return FormatShortDayMonth(period.StartDate);
        }

        if (period.StartDate.Month == 1 && period.StartDate.Day == 1)
        {
            return IsFullCalendarYear(period.StartDate, period.EndDate)
                ? "year"
                : "year to date";
        }

        if (period.StartDate.Day == 1 && period.StartDate.Month == period.EndDate.Month)
        {
            return period.EndDate.Day == DateTime.DaysInMonth(period.EndDate.Year, period.EndDate.Month)
                ? MonthName(period.EndDate.Month)
                : $"{MonthName(period.EndDate.Month)} to date";
        }

        return period.ComparisonLabel;
    }

    private static string CreateComparisonLabel(
        PeriodKind kind,
        DateOnly endDate,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false)
    {
        if (seasonPeriod is not null)
        {
            return MeteorologicalSeasonCalculator.FormatComparisonLabel(seasonPeriod, isSeasonToDate);
        }

        return kind switch
        {
            PeriodKind.LatestSevenDays => $"7 days ending {FormatShortDayMonth(endDate)}",
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? MonthName(endDate.Month)
                : $"{MonthName(endDate.Month)} to date",
            PeriodKind.PreviousMonth => MonthName(endDate.Month),
            PeriodKind.YearToDate => IsCalendarYearEnd(endDate) ? "year" : "year to date",
            PeriodKind.PreviousYear => "year",
            _ => string.Empty,
        };
    }

    private static string CreateComparisonLabelPlural(
        PeriodKind kind,
        DateOnly endDate,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false)
    {
        if (seasonPeriod is not null)
        {
            return MeteorologicalSeasonCalculator.FormatComparisonLabelPlural(seasonPeriod, isSeasonToDate);
        }

        return kind switch
        {
            PeriodKind.LatestSevenDays => $"7-day periods ending {FormatShortDayMonth(endDate)}",
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? $"{MonthName(endDate.Month)}s"
                : $"{MonthName(endDate.Month)}-to-date periods",
            PeriodKind.PreviousMonth => $"{MonthName(endDate.Month)}s",
            PeriodKind.YearToDate => IsCalendarYearEnd(endDate) ? "years" : "year-to-date periods",
            PeriodKind.PreviousYear => "years",
            _ => "comparable periods",
        };
    }

    private static bool IsFullCalendarYear(DateOnly startDate, DateOnly endDate)
    {
        return startDate.Month == 1 &&
            startDate.Day == 1 &&
            endDate.Month == 12 &&
            endDate.Day == 31 &&
            startDate.Year == endDate.Year;
    }

    private static bool IsCalendarYearEnd(DateOnly date)
    {
        return date.Month == 12 && date.Day == 31;
    }

    private static string CreateDailyPeriodTitle(DateOnly date, DateOnly referenceDate, DateOnly today)
    {
        if (date == referenceDate && referenceDate == today)
        {
            return "Today";
        }

        if (date == referenceDate && referenceDate == today.AddDays(-1))
        {
            return "Yesterday";
        }

        if (date == referenceDate.AddDays(-1) && referenceDate == today)
        {
            return "Yesterday";
        }

        return date.Year == today.Year
            ? FormatDayMonth(date)
            : FormatDayMonthYear(date);
    }

    private static string CreateCurrentPeriodLabel(PeriodObservation period)
    {
        if (period.ComparisonMode == PeriodComparisonMode.DailyDate)
        {
            return FormatFullDate(period.StartDate);
        }

        if (period.Kind == PeriodKind.CurrentSeason && period.SeasonPeriod is not null)
        {
            var seasonYear = MeteorologicalSeasonCalculator.FormatSeasonYear(period.SeasonPeriod);
            return period.SeasonPeriod.IsComplete
                ? $"{period.SeasonPeriod.Season} {seasonYear}"
                : $"{period.SeasonPeriod.Season} {seasonYear} to date";
        }

        return period.Title;
    }

    private static string CreateComparableSampleLabel(PeriodObservation period)
    {
        return period.ComparisonMode == PeriodComparisonMode.DailyDate
            ? $"comparable {FormatShortDayMonth(period.StartDate)} observations"
            : "comparable periods";
    }
}
#pragma warning restore SA1201, SA1204
