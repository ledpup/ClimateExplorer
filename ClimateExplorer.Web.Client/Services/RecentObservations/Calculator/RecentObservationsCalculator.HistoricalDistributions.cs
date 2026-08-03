namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Builds, for a given tile period, the population of historical values each metric is
// ranked against - either every prior occurrence of the same calendar date, or every prior
// occurrence of the same date range (a "month", "season", etc.), matched year by year.
public sealed partial class RecentObservationsCalculator
{
    private static IReadOnlyDictionary<string, HistoricalValues> GetHistoricalDistributions(
        HistoricalDailySeries history,
        PeriodObservation period,
        IReadOnlyList<Metric> metrics,
        ComparisonEndMode comparisonEndMode,
        int minimumRankSampleSize)
    {
        return period.ComparisonMode == PeriodComparisonMode.DailyDate
            ? GetHistoricalDailyDateDistributions(history, period, metrics, comparisonEndMode, minimumRankSampleSize)
            : GetHistoricalDailyRangeDistributions(history, period, metrics, comparisonEndMode, minimumRankSampleSize);
    }

    private static IReadOnlyDictionary<string, HistoricalValues> GetHistoricalDailyDateDistributions(
        HistoricalDailySeries history,
        PeriodObservation period,
        IReadOnlyList<Metric> metrics,
        ComparisonEndMode comparisonEndMode,
        int minimumRankSampleSize)
    {
        var sameDate = history.Records
            .Where(x => x.Date.Month == period.StartDate.Month &&
                        x.Date.Day == period.StartDate.Day &&
                        IsEquivalentComparisonYearAllowed(x.Date.Year, period.StartDate.Year, comparisonEndMode))
            .ToList();

        var result = new Dictionary<string, HistoricalValues>();

        foreach (var metric in metrics)
        {
            var values = sameDate
                .Select(x => new HistoricalPeriodValue(metric.Select(x), (short)x.Date.Year, x.Date))
                .Where(x => x.Value.HasValue)
                .ToList();

            result[metric.Key] = new HistoricalValues(
                values,
                history.StartYear,
                values.Count == 0 ? "No comparable historical records are available for this date." : null,
                minimumRankSampleSize);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, HistoricalValues> GetHistoricalDailyRangeDistributions(
        HistoricalDailySeries history,
        PeriodObservation period,
        IReadOnlyList<Metric> metrics,
        ComparisonEndMode comparisonEndMode,
        int minimumRankSampleSize)
    {
        // Group the equivalent historical years once, then compute every metric's
        // aggregate inside the cached groups (single pass over the history).
        var groups = history.Records
            .Where(x => IsWithinEquivalentRange(x.Date, period.StartDate, period.EndDate))
            .Select(x => new
            {
                Record = x,
                EquivalentPeriodYear = GetEquivalentPeriodYear(x.Date, period.StartDate, period.EndDate),
            })
            .Where(x => IsEquivalentComparisonYearAllowed(x.EquivalentPeriodYear, period.StartDate.Year, comparisonEndMode))
            .GroupBy(x => x.EquivalentPeriodYear)
            .Select(group => new EquivalentPeriodGroup(
                group.Key,
                (int)Math.Ceiling(GetEquivalentDayCount(group.Key, period.StartDate, period.EndDate) * MinimumHistoricalCoverage),
                [.. group.Select(x => x.Record)]))
            .ToList();

        var result = new Dictionary<string, HistoricalValues>();

        foreach (var metric in metrics)
        {
            var values = new List<HistoricalPeriodValue>();
            foreach (var group in groups)
            {
                var groupValues = group.Records
                    .Select(record => new MetricObservationValue(record.Date, metric.Select(record)))
                    .Where(x => x.Value.HasValue)
                    .ToList();

                if (groupValues.Count >= group.RequiredDays)
                {
                    var aggregate = Aggregate(groupValues, metric.Aggregation);
                    values.Add(new HistoricalPeriodValue(aggregate.Value, (short)group.Year, aggregate.OccurredOn));
                }
            }

            result[metric.Key] = new HistoricalValues(
                values,
                history.StartYear,
                values.Count == 0 ? "No comparable historical periods are available for this date range." : null,
                minimumRankSampleSize);
        }

        return result;
    }

    private static bool IsEquivalentComparisonYearAllowed(
        int equivalentPeriodYear,
        int observedPeriodYear,
        ComparisonEndMode comparisonEndMode)
    {
        return comparisonEndMode switch
        {
            ComparisonEndMode.ReferenceDate => equivalentPeriodYear < observedPeriodYear,
            ComparisonEndMode.FullDataset => equivalentPeriodYear != observedPeriodYear,
            _ => throw new NotImplementedException(),
        };
    }

    private static bool IsWithinEquivalentRange(DateOnly date, DateOnly templateStart, DateOnly templateEnd)
    {
        var dateMonthDay = (date.Month * 100) + date.Day;
        var startMonthDay = (templateStart.Month * 100) + templateStart.Day;
        var endMonthDay = (templateEnd.Month * 100) + templateEnd.Day;

        return startMonthDay <= endMonthDay
            ? dateMonthDay >= startMonthDay && dateMonthDay <= endMonthDay
            : dateMonthDay >= startMonthDay || dateMonthDay <= endMonthDay;
    }

    private static int GetEquivalentPeriodYear(DateOnly date, DateOnly templateStart, DateOnly templateEnd)
    {
        var dateMonthDay = (date.Month * 100) + date.Day;
        var startMonthDay = (templateStart.Month * 100) + templateStart.Day;
        var endMonthDay = (templateEnd.Month * 100) + templateEnd.Day;

        if (startMonthDay > endMonthDay && dateMonthDay <= endMonthDay)
        {
            return date.Year - 1;
        }

        return date.Year;
    }

    private static int GetEquivalentDayCount(int year, DateOnly templateStart, DateOnly templateEnd)
    {
        var startDate = CreateEquivalentDate(year, templateStart.Month, templateStart.Day);
        var endYear = IsWithinSameCalendarYear(templateStart, templateEnd) ? year : year + 1;
        var endDate = CreateEquivalentDate(endYear, templateEnd.Month, templateEnd.Day);
        return endDate.DayNumber - startDate.DayNumber + 1;
    }

    private static bool IsWithinSameCalendarYear(DateOnly startDate, DateOnly endDate)
    {
        var startMonthDay = (startDate.Month * 100) + startDate.Day;
        var endMonthDay = (endDate.Month * 100) + endDate.Day;
        return startMonthDay <= endMonthDay;
    }

    private static DateOnly CreateEquivalentDate(int year, int month, int day)
    {
        return new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
    }
}
