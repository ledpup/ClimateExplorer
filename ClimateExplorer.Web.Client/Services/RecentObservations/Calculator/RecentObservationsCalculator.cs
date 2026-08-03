#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

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

        if (daily.Count == 0)
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
            daily,
            TemperatureDomain,
            history,
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
        var daily = BuildDailyPrecipitation(dataSet.PrecipitationRecords);
        if (daily.Count == 0)
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
            daily,
            PrecipitationDomain,
            new HistoricalDailySeries(daily, GetStartYear(daily)),
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
        var daily = BuildDailyCo2(dataSet.Co2Records);
        if (daily.Count == 0)
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
            daily,
            Co2Domain,
            new HistoricalDailySeries(daily, GetStartYear(daily)),
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
        previousDayCount = Math.Clamp(previousDayCount, RecentObservationPeriodSelection.DefaultPreviousDayCount, RecentObservationPeriodSelection.MaximumPreviousDayCount);
        previousMonthCount = Math.Clamp(previousMonthCount, 0, RecentObservationPeriodSelection.MaximumPreviousMonthCount);
        previousSeasonCount = Math.Clamp(previousSeasonCount, 0, RecentObservationPeriodSelection.MaximumPreviousSeasonCount);
        previousYearCount = Math.Clamp(previousYearCount, 0, RecentObservationPeriodSelection.MaximumPreviousYearCount);
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
            var distributions = GetHistoricalDistributions(history, period, domain.AllMetrics, comparisonEndMode, minimumRankSampleSize);
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
