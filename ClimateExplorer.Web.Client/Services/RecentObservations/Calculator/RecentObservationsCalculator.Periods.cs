#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Turns a domain's merged daily series into the fixed set of tile periods (previous days,
// latest 7 days, month, season, year - each of the latter three covering both the "to date"
// period, offset 0, and complete past ones, offset 1+) and the titles/labels shown for each one.
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
            RecentObservationPeriodKind.LatestSevenDays,
            domain);

        var currentMonthStart = new DateOnly(referenceDate.Year, referenceDate.Month, 1);
        if (referenceDate.Day != 1)
        {
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, currentMonthStart, referenceDate),
                currentMonthStart,
                referenceDate,
                RecentObservationPeriodKind.Month,
                domain,
                periodOffset: 0);
        }

        foreach (var (startDate, endDate, offset) in GetPreviousPeriods(
            currentMonthStart,
            previousMonthCount,
            (start, stepOffset) => start.AddMonths(-stepOffset),
            start => new DateOnly(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month))))
        {
            AddRangePeriod(periods, GetRecordsInRange(daily, startDate, endDate), startDate, endDate, RecentObservationPeriodKind.Month, domain, periodOffset: offset);
        }

        if (supportsSeasonTiles && latitude.HasValue)
        {
            if (MeteorologicalSeasonCalculator.IsCurrentSeasonToDateMeaningful(referenceDate))
            {
                var currentSeason = MeteorologicalSeasonCalculator.GetCurrentSeasonToDate(referenceDate, latitude.Value);
                AddRangePeriod(
                    periods,
                    GetRecordsInRange(daily, currentSeason.StartDate, currentSeason.EndDate),
                    currentSeason.StartDate,
                    currentSeason.EndDate,
                    RecentObservationPeriodKind.Season,
                    domain,
                    periodOffset: 0,
                    seasonPeriod: currentSeason,
                    isSeasonToDate: !currentSeason.IsComplete);
            }

            var previousSeasons = MeteorologicalSeasonCalculator.GetPreviousSeasons(referenceDate, latitude.Value, previousSeasonCount);
            for (var index = 0; index < previousSeasons.Count; index++)
            {
                var previousSeason = previousSeasons[index];
                AddRangePeriod(
                    periods,
                    GetRecordsInRange(daily, previousSeason.StartDate, previousSeason.EndDate),
                    previousSeason.StartDate,
                    previousSeason.EndDate,
                    RecentObservationPeriodKind.Season,
                    domain,
                    periodOffset: index + 1,
                    seasonPeriod: previousSeason);
            }
        }

        var yearStart = new DateOnly(referenceDate.Year, 1, 1);
        if (referenceDate.Month != 1)
        {
            AddRangePeriod(
                periods,
                GetRecordsInRange(daily, yearStart, referenceDate),
                yearStart,
                referenceDate,
                RecentObservationPeriodKind.Year,
                domain,
                periodOffset: 0);
        }

        foreach (var (startDate, endDate, offset) in GetPreviousPeriods(
            yearStart,
            previousYearCount,
            (start, stepOffset) => new DateOnly(start.Year - stepOffset, 1, 1),
            start => new DateOnly(start.Year, 12, 31)))
        {
            AddRangePeriod(periods, GetRecordsInRange(daily, startDate, endDate), startDate, endDate, RecentObservationPeriodKind.Year, domain, periodOffset: offset);
        }

        return periods;
    }

    // Shared "walk back N whole units from an anchor" shape behind the month and year loops
    // above - each supplies only its own step-back and end-of-period rules. Season's own
    // previous-period walk stays in MeteorologicalSeasonCalculator (Core, public, independently
    // tested) rather than being rebuilt on top of this - see the design doc for why.
    private static IEnumerable<(DateOnly StartDate, DateOnly EndDate, int Offset)> GetPreviousPeriods(
        DateOnly anchorStart,
        int count,
        Func<DateOnly, int, DateOnly> stepBack,
        Func<DateOnly, DateOnly> getEndDate)
    {
        for (var offset = 1; offset <= count; offset++)
        {
            var startDate = stepBack(anchorStart, offset);
            yield return (startDate, getEndDate(startDate), offset);
        }
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
            RecentObservationPeriodKind.Daily,
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
        RecentObservationPeriodKind kind,
        MetricDomain domain,
        int? periodOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false,
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
            CreatePeriodTitle(kind, startDate, endDate, periodOffset, seasonPeriod, isSeasonToDate),
            CreateComparisonLabel(kind, endDate, periodOffset, seasonPeriod, isSeasonToDate),
            CreateComparisonLabelPlural(kind, endDate, periodOffset, seasonPeriod, isSeasonToDate),
            startDate,
            endDate,
            completeness,
            kind,
            PeriodComparisonMode.DailyRange,
            periodOffset,
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

    private static int GetDayCount(DateOnly startDate, DateOnly endDate)
    {
        return endDate.DayNumber - startDate.DayNumber + 1;
    }

    private static string CreatePeriodTitle(
        RecentObservationPeriodKind kind,
        DateOnly startDate,
        DateOnly endDate,
        int? periodOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false)
    {
        if (seasonPeriod is not null)
        {
            return MeteorologicalSeasonCalculator.FormatTitle(seasonPeriod, isSeasonToDate);
        }

        return kind switch
        {
            RecentObservationPeriodKind.LatestSevenDays => "Latest 7 days",
            RecentObservationPeriodKind.Month when periodOffset == 0 => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? $"{MonthName(endDate.Month)} {endDate.Year}"
                : $"{MonthName(endDate.Month)} {endDate.Year} to date",
            RecentObservationPeriodKind.Month when periodOffset == 1 => $"Last month - {MonthName(startDate.Month)} {startDate.Year}",
            RecentObservationPeriodKind.Month => $"{MonthName(startDate.Month)} {startDate.Year}",
            RecentObservationPeriodKind.Year when periodOffset == 0 => IsCalendarYearEnd(endDate)
                ? endDate.Year.ToString(CultureInfo.InvariantCulture)
                : $"{endDate.Year} to date",
            RecentObservationPeriodKind.Year when periodOffset == 1 => $"Last year - {endDate.Year}",
            RecentObservationPeriodKind.Year => startDate.Year.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty,
        };
    }

    private static string CreateHistoricalContextLabel(PeriodObservation period)
    {
        if (period.Kind == RecentObservationPeriodKind.Season)
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
        RecentObservationPeriodKind kind,
        DateOnly endDate,
        int? periodOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false)
    {
        if (seasonPeriod is not null)
        {
            return MeteorologicalSeasonCalculator.FormatComparisonLabel(seasonPeriod, isSeasonToDate);
        }

        return kind switch
        {
            RecentObservationPeriodKind.LatestSevenDays => $"7 days ending {FormatShortDayMonth(endDate)}",
            RecentObservationPeriodKind.Month when periodOffset == 0 => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? MonthName(endDate.Month)
                : $"{MonthName(endDate.Month)} to date",
            RecentObservationPeriodKind.Month => MonthName(endDate.Month),
            RecentObservationPeriodKind.Year when periodOffset == 0 => IsCalendarYearEnd(endDate) ? "year" : "year to date",
            RecentObservationPeriodKind.Year => "year",
            _ => string.Empty,
        };
    }

    private static string CreateComparisonLabelPlural(
        RecentObservationPeriodKind kind,
        DateOnly endDate,
        int? periodOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false)
    {
        if (seasonPeriod is not null)
        {
            return MeteorologicalSeasonCalculator.FormatComparisonLabelPlural(seasonPeriod, isSeasonToDate);
        }

        return kind switch
        {
            RecentObservationPeriodKind.LatestSevenDays => $"7-day periods ending {FormatShortDayMonth(endDate)}",
            RecentObservationPeriodKind.Month when periodOffset == 0 => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? $"{MonthName(endDate.Month)}s"
                : $"{MonthName(endDate.Month)}-to-date periods",
            RecentObservationPeriodKind.Month => $"{MonthName(endDate.Month)}s",
            RecentObservationPeriodKind.Year when periodOffset == 0 => IsCalendarYearEnd(endDate) ? "years" : "year-to-date periods",
            RecentObservationPeriodKind.Year => "years",
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

        if (period.Kind == RecentObservationPeriodKind.Season && period.PeriodOffset == 0 && period.SeasonPeriod is not null)
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
