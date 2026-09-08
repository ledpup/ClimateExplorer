#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using System.Runtime.CompilerServices;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Split by concern across the RecentObservationsCalculator.*.cs files in this folder:
// this file is the public entry point and top-level orchestration; DailySeries builds the
// raw per-day observation lists; Periods/PeriodLabels turn those into the tile periods and
// their titles; Metrics/MetricDefinitions describe what each domain measures and how;
// HistoricalDistributions computes the comparison population for a period; TileBuilding,
// MetricGroups, Variation and Trend assemble the view model sections; Formatting holds
// shared string/number formatting; Types holds the private records/enums used throughout.
public sealed partial class RecentObservationsCalculator : IRecentObservationsCalculator
{
    private const int LatestSevenDaysLength = 7;
    private const double MinimumHistoricalCoverage = 0.9d;
    private const int RecentTrendWindowYears = 30;

    private readonly ConditionalWeakTable<RecentObservationsDataSet, PreparedDailySeries> dailySeriesCache = new();

    private readonly ConditionalWeakTable<HistoricalDailySeries, Dictionary<HistoricalDistributionCacheKey, IReadOnlyDictionary<string, HistoricalValues>>> historicalDistributionsCache = new();

    private readonly TimeProvider timeProvider;

    public RecentObservationsCalculator(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public RecentObservationsTabResult Calculate(
        double? latitude,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options)
    {
        if (!dataSet.IsSupported)
        {
            return new RecentObservationsTabResult
            {
                IsSupported = false,
                EmptyMessage = dataSet.UnsupportedMessage,
                SourceMetadata = dataSet.SourceMetadata,
                ComparisonEndMode = options.ComparisonEndMode,
            };
        }

        return dataSet.DomainKey switch
        {
            ObservationDomainCatalog.TemperatureKey => CalculateTemperature(latitude, dataSet, options),
            ObservationDomainCatalog.PrecipitationKey => CalculatePrecipitation(latitude, dataSet, options),
            ObservationDomainCatalog.Co2Key => CalculateCo2(dataSet, options),
            _ => throw new NotSupportedException($"Unknown observation domain '{dataSet.DomainKey}'."),
        };
    }

    private RecentObservationsTabResult CalculateTemperature(
        double? latitude,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options)
    {
        var series = GetOrBuildDailySeries(dataSet);
        if (series.Daily.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = dataSet.EmptyMessage,
                SourceMetadata = dataSet.SourceMetadata,
                ComparisonEndMode = options.ComparisonEndMode,
            };
        }

        return BuildTiles(
            latitude,
            series.Daily,
            TemperatureDomain,
            series.History,
            options.ReferenceDate,
            options.ComparisonEndMode,
            options.MinimumRankSampleSize,
            options.PreviousDayCount,
            options.PreviousMonthCount,
            options.PreviousSeasonCount,
            options.PreviousYearCount,
            dataSet.NoPeriodsMessage,
            dataSet.EmptyMessage,
            dataSet.SourceMetadata,
            supportsSeasonTiles: true);
    }

    private RecentObservationsTabResult CalculatePrecipitation(
        double? latitude,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options)
    {
        var series = GetOrBuildDailySeries(dataSet);
        if (series.Daily.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = dataSet.EmptyMessage,
                SourceMetadata = dataSet.SourceMetadata,
                ComparisonEndMode = options.ComparisonEndMode,
            };
        }

        return BuildTiles(
            latitude,
            series.Daily,
            PrecipitationDomain,
            series.History,
            options.ReferenceDate,
            options.ComparisonEndMode,
            options.MinimumRankSampleSize,
            options.PreviousDayCount,
            options.PreviousMonthCount,
            options.PreviousSeasonCount,
            options.PreviousYearCount,
            dataSet.NoPeriodsMessage,
            dataSet.EmptyMessage,
            dataSet.SourceMetadata,
            supportsSeasonTiles: true);
    }

    private RecentObservationsTabResult CalculateCo2(
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options)
    {
        var series = GetOrBuildDailySeries(dataSet);
        if (series.Daily.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = dataSet.EmptyMessage,
                SourceMetadata = dataSet.SourceMetadata,
                ComparisonEndMode = options.ComparisonEndMode,
            };
        }

        return BuildTiles(
            null,
            series.Daily,
            Co2Domain,
            series.History,
            options.ReferenceDate,
            options.ComparisonEndMode,
            options.MinimumRankSampleSize,
            options.PreviousDayCount,
            options.PreviousMonthCount,
            options.PreviousSeasonCount,
            options.PreviousYearCount,
            dataSet.NoPeriodsMessage,
            dataSet.EmptyMessage,
            dataSet.SourceMetadata,
            supportsSeasonTiles: false);
    }

    // See the dailySeriesCache field for why this is memoized per dataset instance.
    private PreparedDailySeries GetOrBuildDailySeries(RecentObservationsDataSet dataSet)
    {
        return dailySeriesCache.GetValue(dataSet, BuildDailySeries);
    }

    private static PreparedDailySeries BuildDailySeries(RecentObservationsDataSet dataSet)
    {
        return dataSet.DomainKey switch
        {
            ObservationDomainCatalog.TemperatureKey => BuildTemperatureDailySeries(dataSet),
            ObservationDomainCatalog.PrecipitationKey => BuildSingleValueDailySeries(BuildDailyPrecipitation(dataSet.PrecipitationRecords)),
            ObservationDomainCatalog.Co2Key => BuildSingleValueDailySeries(BuildDailyCo2(dataSet.Co2Records)),
            _ => throw new NotSupportedException($"Unknown observation domain '{dataSet.DomainKey}'."),
        };
    }

    private static PreparedDailySeries BuildTemperatureDailySeries(RecentObservationsDataSet dataSet)
    {
        var daily = BuildDailyTemperature(dataSet.TemperatureMaxRecords, dataSet.TemperatureMinRecords);
        var meanHistoryRecords = dataSet.HasHistoricalTemperatureMaxMin
            ? new List<DailyObservation>()
            : BuildDailyTemperatureMean(dataSet.TemperatureMeanRecords);
        var meanHistory = new HistoricalDailySeries(meanHistoryRecords, GetStartYear(meanHistoryRecords));
        var history = dataSet.HasHistoricalTemperatureMaxMin && daily.Count > 0
            ? new HistoricalDailySeries(daily, GetStartYear(daily))
            : meanHistory;

        if (!dataSet.HasHistoricalTemperatureMaxMin && history.Records.Count > 0)
        {
            daily = MergeDailyObservations(history.Records, daily);
        }

        return new PreparedDailySeries(daily, history);
    }

    // Precipitation and CO2 rank each period against every other year's occurrence of the same
    // date range, drawn from the same single merged series - so, unlike temperature, "daily" and
    // "history" are just two names for the one list here.
    private static PreparedDailySeries BuildSingleValueDailySeries(List<DailyObservation> daily)
    {
        return new PreparedDailySeries(daily, new HistoricalDailySeries(daily, GetStartYear(daily)));
    }

    private RecentObservationsTabResult BuildTiles(
        double? latitude,
        List<DailyObservation> daily,
        MetricDomain domain,
        HistoricalDailySeries history,
        DateOnly? requestedReferenceDate,
        ComparisonEndMode comparisonEndMode,
        int minimumRankSampleSize,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        int previousYearCount,
        string noPeriodsMessage,
        string emptyMessage,
        IReadOnlyList<RecentObservationSourceMetadata> sourceMetadata,
        bool supportsSeasonTiles)
    {
        previousDayCount = Math.Max(previousDayCount, RecentObservationPeriodSelection.DefaultPreviousDayCount);
        previousMonthCount = Math.Max(previousMonthCount, 0);
        previousSeasonCount = Math.Max(previousSeasonCount, 0);
        previousYearCount = Math.Max(previousYearCount, 0);
        minimumRankSampleSize = Math.Max(1, minimumRankSampleSize);

        var referenceDate = ResolveReferenceDate(daily, requestedReferenceDate);
        if (referenceDate.ReferenceDate is null)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = requestedReferenceDate.HasValue
                    ? $"No observations are available on or before {FormatDayMonthYear(requestedReferenceDate.Value)}."
                    : emptyMessage,
                RequestedReferenceDate = requestedReferenceDate,
                MinimumReferenceDate = referenceDate.MinimumReferenceDate,
                MaximumReferenceDate = referenceDate.MaximumReferenceDate,
                SourceMetadata = sourceMetadata,
                ComparisonEndMode = comparisonEndMode,
            };
        }

        var today = GetToday();
        var observationsAsOfReferenceDate = daily
            .Where(x => x.Date <= referenceDate.ReferenceDate.Value)
            .OrderBy(x => x.Date)
            .ToList();

        // Month/season/year periods are generated by unconditionally walking backwards from the
        // reference date (unlike day periods, which are naturally bounded by Take() over the
        // available records), so an effectively-unlimited count (int.MaxValue) must be clamped to
        // how far back data actually exists - otherwise walking back that many months/years
        // overflows DateOnly's range long before running out of iterations.
        if (observationsAsOfReferenceDate.Count > 0)
        {
            var earliestDataDate = observationsAsOfReferenceDate[0].Date;
            var currentMonthStart = new DateOnly(referenceDate.ReferenceDate.Value.Year, referenceDate.ReferenceDate.Value.Month, 1);
            var monthsOfDataAvailable = Math.Max(0, ((currentMonthStart.Year - earliestDataDate.Year) * 12) + currentMonthStart.Month - earliestDataDate.Month);

            previousMonthCount = Math.Min(previousMonthCount, monthsOfDataAvailable);
            previousSeasonCount = Math.Min(previousSeasonCount, (monthsOfDataAvailable / 3) + 1);
            previousYearCount = Math.Min(previousYearCount, Math.Max(0, referenceDate.ReferenceDate.Value.Year - earliestDataDate.Year));
        }

        var periods = BuildPeriods(
            observationsAsOfReferenceDate,
            referenceDate.ReferenceDate.Value,
            today,
            latitude,
            domain,
            previousDayCount,
            previousMonthCount,
            previousSeasonCount,
            previousYearCount,
            supportsSeasonTiles);
        if (periods.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = noPeriodsMessage,
                RequestedReferenceDate = requestedReferenceDate,
                ReferenceDate = referenceDate.ReferenceDate,
                MinimumReferenceDate = referenceDate.MinimumReferenceDate,
                MaximumReferenceDate = referenceDate.MaximumReferenceDate,
                ReferenceDateNote = CreateReferenceDateNote(requestedReferenceDate, referenceDate.ReferenceDate.Value),
                SourceMetadata = sourceMetadata,
                ComparisonEndMode = comparisonEndMode,
            };
        }

        var tiles = new List<RecentObservationTileViewModel>();

        foreach (var period in periods)
        {
            var distributions = GetOrBuildHistoricalDistributions(history, period, domain.AllMetrics, comparisonEndMode, minimumRankSampleSize);
            tiles.Add(BuildTile(period, domain, distributions));
        }

        return new RecentObservationsTabResult
        {
            EmptyMessage = emptyMessage,
            RequestedReferenceDate = requestedReferenceDate,
            ReferenceDate = referenceDate.ReferenceDate,
            MinimumReferenceDate = referenceDate.MinimumReferenceDate,
            MaximumReferenceDate = referenceDate.MaximumReferenceDate,
            ReferenceDateNote = CreateReferenceDateNote(requestedReferenceDate, referenceDate.ReferenceDate.Value),
            SourceMetadata = sourceMetadata,
            ComparisonEndMode = comparisonEndMode,
            Tiles = tiles,
        };
    }

    private static ReferenceDateResolution ResolveReferenceDate(
        IReadOnlyCollection<DailyObservation> daily,
        DateOnly? requestedReferenceDate)
    {
        if (daily.Count == 0)
        {
            return new ReferenceDateResolution(null, null, null);
        }

        var dates = daily
            .Select(x => x.Date)
            .Distinct()
            .Order()
            .ToList();
        var minimumReferenceDate = dates[0];
        var maximumReferenceDate = dates[^1];
        var referenceDate = requestedReferenceDate.HasValue
            ? dates.LastOrDefault(x => x <= requestedReferenceDate.Value)
            : maximumReferenceDate;

        return new ReferenceDateResolution(
            referenceDate == default ? null : referenceDate,
            minimumReferenceDate,
            maximumReferenceDate);
    }

    private static string? CreateReferenceDateNote(DateOnly? requestedReferenceDate, DateOnly referenceDate)
    {
        return requestedReferenceDate.HasValue && requestedReferenceDate.Value != referenceDate
            ? $"No observation is available for {FormatDayMonthYear(requestedReferenceDate.Value)}; showing {FormatDayMonthYear(referenceDate)} instead."
            : null;
    }

    private DateOnly GetToday()
    {
        return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    }
}
#pragma warning restore SA1201, SA1204
