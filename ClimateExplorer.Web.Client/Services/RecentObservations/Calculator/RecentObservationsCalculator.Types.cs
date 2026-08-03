#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Calculators;

// The private records/enums shared across the RecentObservationsCalculator.*.cs files.
public sealed partial class RecentObservationsCalculator
{
    private sealed record DailyObservation(DateOnly Date, double? Max, double? Min, double? Mean, double? Precipitation, double? Co2 = null);

    private sealed record ReferenceDateResolution(
        DateOnly? ReferenceDate,
        DateOnly? MinimumReferenceDate,
        DateOnly? MaximumReferenceDate);

    private sealed record HistoricalDailySeries(List<DailyObservation> Records, int? StartYear);

    private sealed record EquivalentPeriodGroup(int Year, int RequiredDays, IReadOnlyList<DailyObservation> Records);

    private sealed record PeriodObservation(
        string Title,
        string ComparisonLabel,
        string ComparisonLabelPlural,
        DateOnly StartDate,
        DateOnly EndDate,
        ObservationCompleteness Completeness,
        PeriodKind Kind,
        PeriodComparisonMode ComparisonMode,
        int? PeriodOffset,
        string? Note,
        IReadOnlyDictionary<string, MetricObservationValue> MetricValues,
        MeteorologicalSeasonPeriod? SeasonPeriod = null);

    private sealed record ObservationCompleteness(int AvailableObservationCount, int ExpectedObservationCount)
    {
        public static ObservationCompleteness CompleteDay { get; } = new(1, 1);
    }

    private sealed record PreviousMonthPeriod(DateOnly StartDate, DateOnly EndDate, int Offset);

    private sealed record PreviousYearPeriod(DateOnly StartDate, DateOnly EndDate, int Offset);

    private sealed record CurrentPeriod(DateOnly StartDate, DateOnly EndDate);

    private sealed record PreviousDayPeriod<TRecord>(TRecord Record, string Title, int Offset);

    private sealed record HistoricalValues(
        List<HistoricalPeriodValue> PeriodValues,
        int? StartYear,
        string? UnavailableReason,
        int MinimumRankSampleSize)
    {
        public List<double?> Values => [.. PeriodValues.Select(x => x.Value)];

        public List<double> FiniteValues => [.. PeriodValues
            .Where(x => x.Value.HasValue && double.IsFinite(x.Value.Value))
            .Select(x => x.Value!.Value)];

        public int ComparablePeriodCount => PeriodValues.Count(x => x.Value.HasValue && double.IsFinite(x.Value.Value));

        public bool CanShowHistoricalRecord => ComparablePeriodCount >= 1;

        public bool CanShowHistoricalRange => ComparablePeriodCount >= 2;

        public bool CanShowRank => ComparablePeriodCount >= MinimumRankSampleSize;

        public bool CanShowPercentile => CanShowRank;

        public HistoricalPeriodValue? MaxValue => PeriodValues
            .Where(x => x.Value.HasValue && double.IsFinite(x.Value.Value))
            .OrderByDescending(x => x.Value!.Value)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.OccurredOn)
            .FirstOrDefault();

        public HistoricalPeriodValue? MinValue => PeriodValues
            .Where(x => x.Value.HasValue && double.IsFinite(x.Value.Value))
            .OrderBy(x => x.Value!.Value)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.OccurredOn)
            .FirstOrDefault();
    }

    private sealed record HistoricalPeriodValue(double? Value, short? Year, DateOnly? OccurredOn);

    private sealed record MetricObservationValue(DateOnly? OccurredOn, double? Value);

    private enum PeriodKind
    {
        Daily,
        LatestSevenDays,
        CurrentMonth,
        PreviousMonth,
        CurrentSeason,
        PreviousSeason,
        YearToDate,
        PreviousYear,
    }

    private enum PeriodComparisonMode
    {
        DailyDate,
        DailyRange,
    }
}
#pragma warning restore SA1201, SA1204
