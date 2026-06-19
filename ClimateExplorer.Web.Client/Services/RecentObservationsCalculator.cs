#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;

public sealed class RecentObservationsCalculator : IRecentObservationsCalculator
{
    private const int LatestSevenDaysLength = 7;
    private const double MinimumHistoricalCoverage = 0.9d;

    private readonly TimeProvider timeProvider;

    public RecentObservationsCalculator(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public RecentObservationsTabResult Calculate(
        Location location,
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

        return dataSet.Tab switch
        {
            RecentObservationsTab.Temperature => CalculateTemperature(location, dataSet, options),
            RecentObservationsTab.Precipitation => CalculatePrecipitation(location, dataSet, options),
            _ => throw new NotImplementedException(),
        };
    }

    private RecentObservationsTabResult CalculateTemperature(
        Location location,
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
            location,
            daily,
            TemperatureDomain,
            history,
            options.ReferenceDate,
            options.ComparisonEndMode,
            options.MinimumRankSampleSize,
            options.PreviousDayCount,
            options.PreviousMonthCount,
            options.PreviousSeasonCount,
            dataSet.NoPeriodsMessage,
            dataSet.EmptyMessage,
            dataSet.SourceMetadata);
    }

    private RecentObservationsTabResult CalculatePrecipitation(
        Location location,
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
            location,
            daily,
            PrecipitationDomain,
            new HistoricalDailySeries(daily, GetStartYear(daily)),
            options.ReferenceDate,
            options.ComparisonEndMode,
            options.MinimumRankSampleSize,
            options.PreviousDayCount,
            options.PreviousMonthCount,
            options.PreviousSeasonCount,
            dataSet.NoPeriodsMessage,
            dataSet.EmptyMessage,
            dataSet.SourceMetadata);
    }

    private RecentObservationsTabResult BuildTiles(
        Location location,
        List<DailyObservation> daily,
        MetricDomain domain,
        HistoricalDailySeries history,
        DateOnly? requestedReferenceDate,
        ComparisonEndMode comparisonEndMode,
        int minimumRankSampleSize,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        string noPeriodsMessage,
        string emptyMessage,
        IReadOnlyList<RecentObservationSourceMetadata> sourceMetadata)
    {
        previousDayCount = Math.Clamp(previousDayCount, RecentObservationPeriodSelection.DefaultPreviousDayCount, RecentObservationPeriodSelection.MaximumPreviousDayCount);
        previousMonthCount = Math.Clamp(previousMonthCount, 0, RecentObservationPeriodSelection.MaximumPreviousMonthCount);
        previousSeasonCount = Math.Clamp(previousSeasonCount, 0, RecentObservationPeriodSelection.MaximumPreviousSeasonCount);
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
            location.Coordinates.Latitude,
            domain,
            previousDayCount,
            previousMonthCount,
            previousSeasonCount);
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

    private static List<DailyObservation> BuildDailyTemperature(IEnumerable<DataRecord> maxRecords, IEnumerable<DataRecord> minRecords)
    {
        var minByDate = minRecords
            .Where(x => x.Date.HasValue && x.Value.HasValue)
            .ToDictionary(x => x.Date!.Value, x => x.Value!.Value);

        return [.. maxRecords
            .Where(x => x.Date.HasValue && x.Value.HasValue && minByDate.ContainsKey(x.Date!.Value))
            .Select(x =>
            {
                var date = x.Date!.Value;
                var max = x.Value!.Value;
                var min = minByDate[date];
                return new DailyObservation(date, max, min, (max + min) / 2d, null);
            })
            .OrderBy(x => x.Date)];
    }

    private static List<DailyObservation> BuildDailyPrecipitation(IEnumerable<DataRecord> records)
    {
        return [.. records
            .Where(x => x.Date.HasValue && x.Value.HasValue)
            .Select(x => new DailyObservation(x.Date!.Value, null, null, null, x.Value!.Value))
            .OrderBy(x => x.Date)];
    }

    private static List<DailyObservation> BuildDailyTemperatureMean(IEnumerable<DataRecord> records)
    {
        return [.. records
            .Where(x => x.Date.HasValue && x.Value.HasValue)
            .Select(x => new DailyObservation(x.Date!.Value, null, null, x.Value!.Value, null))
            .OrderBy(x => x.Date)];
    }

    private static List<DataRecord> MergeDailyDataRecords(
        IEnumerable<DataRecord> historicalRecords,
        IEnumerable<DataRecord> recentRecords)
    {
        var recordsByDate = new SortedDictionary<DateOnly, DataRecord>();

        foreach (var record in historicalRecords.Where(x => x.Date.HasValue && x.Value.HasValue))
        {
            recordsByDate[record.Date!.Value] = record;
        }

        foreach (var record in recentRecords.Where(x => x.Date.HasValue && x.Value.HasValue))
        {
            recordsByDate[record.Date!.Value] = record;
        }

        return [.. recordsByDate.Values];
    }

    private static List<DailyObservation> MergeDailyObservations(
        IEnumerable<DailyObservation> historicalRecords,
        IEnumerable<DailyObservation> recentRecords)
    {
        var recordsByDate = new SortedDictionary<DateOnly, DailyObservation>();

        foreach (var record in historicalRecords)
        {
            recordsByDate[record.Date] = record;
        }

        foreach (var record in recentRecords)
        {
            recordsByDate[record.Date] = record;
        }

        return [.. recordsByDate.Values];
    }

    private static List<DailyObservation> GetRecordsInRange(
        IEnumerable<DailyObservation> records,
        DateOnly startDate,
        DateOnly endDate)
    {
        return [.. records.Where(x => x.Date >= startDate && x.Date <= endDate)];
    }

    private static List<PeriodObservation> BuildPeriods(
        List<DailyObservation> daily,
        DateOnly referenceDate,
        DateOnly today,
        double latitude,
        MetricDomain domain,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount)
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

        var monthStart = new DateOnly(referenceDate.Year, referenceDate.Month, 1);
        AddRangePeriod(periods, GetRecordsInRange(daily, monthStart, referenceDate), monthStart, referenceDate, PeriodKind.CurrentMonth, domain);

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

        var currentSeasonToDate = GetCurrentSeasonToDatePeriod(referenceDate, latitude);
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
                isSeasonToDate: true);
        }

        var previousSeasons = MeteorologicalSeasonCalculator.GetPreviousSeasons(referenceDate, latitude, previousSeasonCount);
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

        var ytdStart = new DateOnly(referenceDate.Year, 1, 1);
        AddRangePeriod(periods, GetRecordsInRange(daily, ytdStart, referenceDate), ytdStart, referenceDate, PeriodKind.YearToDate, domain);

        return periods;
    }

    private static MeteorologicalSeasonPeriod? GetCurrentSeasonToDatePeriod(DateOnly referenceDate, double latitude)
    {
        if (!MeteorologicalSeasonCalculator.IsCurrentSeasonToDateMeaningful(referenceDate))
        {
            return null;
        }

        return MeteorologicalSeasonCalculator.GetCurrentSeason(referenceDate, latitude) with { EndDate = referenceDate };
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
            ComputeMetrics(records, domain)));
    }

    private static IReadOnlyDictionary<string, double> ComputeMetrics(IReadOnlyList<DailyObservation> records, MetricDomain domain)
    {
        var result = new Dictionary<string, double>();

        foreach (var metric in domain.AllMetrics)
        {
            var values = records
                .Select(metric.Select)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (values.Count > 0)
            {
                result[metric.Key] = Aggregate(values, metric.Aggregation);
            }
        }

        return result;
    }

    private static double Aggregate(IReadOnlyList<double> values, MetricAggregation aggregation)
    {
        return aggregation switch
        {
            MetricAggregation.Mean => values.Average(),
            MetricAggregation.Sum => values.Sum(),
            MetricAggregation.Max => values.Max(),
            MetricAggregation.Min => values.Min(),
            _ => throw new NotImplementedException(),
        };
    }

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
                .Select(x => new HistoricalPeriodValue(metric.Select(x), (short)x.Date.Year))
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
                    .Select(metric.Select)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();

                if (groupValues.Count >= group.RequiredDays)
                {
                    values.Add(new HistoricalPeriodValue(Aggregate(groupValues, metric.Aggregation), (short)group.Year));
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

    private DateOnly GetToday()
    {
        return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    }

    private static RecentObservationTileViewModel BuildTile(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions)
    {
        var historicalValues = distributions[domain.Primary.Key];
        var primaryValue = period.MetricValues[domain.Primary.Key];
        var ranking = RecentObservationComparison.Rank(primaryValue, historicalValues.Values);
        var singular = period.Completeness.AvailableObservationCount == 1;
        var stats = new List<RecentObservationStatViewModel>();
        var supportingStats = new List<RecentObservationStatViewModel>();

        foreach (var metric in domain.Supporting)
        {
            if (period.MetricValues.TryGetValue(metric.Key, out var value))
            {
                var status = GetRecordStatus(value, distributions.GetValueOrDefault(metric.Key));
                supportingStats.Add(new RecentObservationStatViewModel
                {
                    Label = singular ? metric.SingularLabel : metric.PluralLabel,
                    Value = metric.Format(value),
                    RecordStatus = status,
                    RecordStatusText = FormatCollapsedRecordStatus(status),
                });
            }
        }

        var primaryStatus = ranking is null
            ? RecentObservationRecordStatus.None
            : RecentObservationComparison.DetermineRecordStatus(ranking);

        if (ranking is not null)
        {
            stats.Add(new RecentObservationStatViewModel { Label = "Historical average", Value = domain.Primary.Format(ranking.HistoricalAverage) });
            stats.Add(new RecentObservationStatViewModel { Label = "Anomaly", Value = FormatAnomaly(ranking.Anomaly, domain.Primary) });
        }

        var showHistoricalRange = ranking is not null && historicalValues.CanShowHistoricalRange;
        var historicalContext = showHistoricalRange ? CreateHistoricalContextLabel(period) : null;
        var metricGroupLabel = CreateMetricGroupLabel(period);
        var showMin = domain.ShowHistoricalMin;

        return new RecentObservationTileViewModel
        {
            PeriodKind = ToTilePeriodKind(period.Kind),
            PeriodOffset = period.PeriodOffset,
            PeriodStartDate = period.StartDate,
            PeriodEndDate = period.EndDate,
            PeriodTitle = period.Title,
            MetricGroupLabel = metricGroupLabel,
            Headline = BuildTileHeadline(period, domain, historicalValues, ranking),
            PercentileSentence = BuildPercentileSentence(period, domain, historicalValues, ranking),
            PrimaryLabel = domain.PrimaryLabel,
            PrimaryValue = domain.Primary.Format(primaryValue),
            PrimaryRecordStatus = primaryStatus,
            PrimaryRecordStatusText = FormatCollapsedRecordStatus(primaryStatus),
            HistoricalMaxLabel = showHistoricalRange ? $"{domain.HistoricalMaxWord} {historicalContext}" : null,
            HistoricalMaxValue = showHistoricalRange ? domain.Primary.Format(ranking!.HistoricalMax) : null,
            HistoricalMaxOccurred = showHistoricalRange ? FormatHistoricalOccurrence(historicalValues.MaxValue) : null,
            HistoricalMinLabel = showHistoricalRange && showMin ? $"{domain.HistoricalMinWord} {historicalContext}" : null,
            HistoricalMinValue = showHistoricalRange && showMin ? domain.Primary.Format(ranking!.HistoricalMin) : null,
            HistoricalMinOccurred = showHistoricalRange && showMin ? FormatHistoricalOccurrence(historicalValues.MinValue) : null,
            HasComparison = ranking is not null,
            Tone = domain.GetTone(ranking),
            Note = CombineNotes(period.Note, BuildLimitedHistoryNote(period, historicalValues, ranking)),
            Stats = stats,
            SupportingStats = supportingStats,
            MetricGroups = BuildMetricGroups(period, domain, distributions, metricGroupLabel),
            ComparablePeriodCount = historicalValues.ComparablePeriodCount,
            CanShowHistoricalRecord = historicalValues.CanShowHistoricalRecord,
            CanShowHistoricalRange = historicalValues.CanShowHistoricalRange,
            CanShowRank = historicalValues.CanShowRank,
            CanShowPercentile = historicalValues.CanShowPercentile,
            AvailableObservationCount = period.Completeness.AvailableObservationCount,
            ExpectedObservationCount = period.Completeness.ExpectedObservationCount,
        };
    }

    private static RecentObservationRecordStatus GetRecordStatus(double currentValue, HistoricalValues? distribution)
    {
        var ranking = distribution is null
            ? null
            : RecentObservationComparison.Rank(currentValue, distribution.Values);

        return ranking is null
            ? RecentObservationRecordStatus.None
            : RecentObservationComparison.DetermineRecordStatus(ranking);
    }

    private static string? FormatCollapsedRecordStatus(RecentObservationRecordStatus status)
    {
        return status switch
        {
            RecentObservationRecordStatus.NewRecord => "NEW RECORD",
            RecentObservationRecordStatus.EqualRecord => "EQUAL RECORD",
            _ => null,
        };
    }

    private static string BuildTileHeadline(
        PeriodObservation period,
        MetricDomain domain,
        HistoricalValues historicalValues,
        RecentObservationComparisonResult? ranking)
    {
        if (ranking is null)
        {
            return "Comparison unavailable";
        }

        return historicalValues.CanShowRank
            ? domain.BuildHeadline(period.ComparisonLabel, ranking)
            : BuildLimitedSampleHeadline(period, domain, ranking);
    }

    private static string BuildLimitedSampleHeadline(
        PeriodObservation period,
        MetricDomain domain,
        RecentObservationComparisonResult ranking)
    {
        var sampleLabel = CreateComparableSampleLabel(period);

        if (ranking.IsNewHighRecord)
        {
            return $"{domain.HistoricalMaxWord} of {ranking.ComparableCount} {sampleLabel}";
        }

        if (ranking.IsNewLowRecord)
        {
            return $"{domain.HistoricalMinWord} of {ranking.ComparableCount} {sampleLabel}";
        }

        if (ranking.IsTiedHighRecord && ranking.HighRank == 1)
        {
            return $"Equal {LowerFirst(domain.HistoricalMaxWord)} of {ranking.ComparableCount} {sampleLabel}";
        }

        if (ranking.IsTiedLowRecord && ranking.LowRank == 1)
        {
            return $"Equal {LowerFirst(domain.HistoricalMinWord)} of {ranking.ComparableCount} {sampleLabel}";
        }

        return "Limited historical comparison";
    }

    private static string BuildPercentileSentence(
        PeriodObservation period,
        MetricDomain domain,
        HistoricalValues historicalValues,
        RecentObservationComparisonResult? ranking)
    {
        if (ranking is null)
        {
            return historicalValues.UnavailableReason ?? "No comparable historical data is available for this comparison.";
        }

        return historicalValues.CanShowPercentile
            ? domain.BuildPercentileSentence(period.ComparisonLabelPlural, historicalValues.StartYear, ranking)
            : $"Ranking unavailable: only {FormatHistoricalSampleCount(historicalValues.ComparablePeriodCount, period)}.";
    }

    private static string? BuildLimitedHistoryNote(
        PeriodObservation period,
        HistoricalValues historicalValues,
        RecentObservationComparisonResult? ranking)
    {
        return ranking is not null && !historicalValues.CanShowRank
            ? $"Limited history: comparison based on {FormatHistoricalSampleCount(historicalValues.ComparablePeriodCount, period)}."
            : null;
    }

    private static string? CombineNotes(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.IsNullOrWhiteSpace(second) ? null : second;
        }

        if (string.IsNullOrWhiteSpace(second) || first.Contains(second, StringComparison.Ordinal))
        {
            return first;
        }

        return $"{first} {second}";
    }

    private static string FormatHistoricalSampleCount(int count, PeriodObservation period)
    {
        var noun = period.ComparisonMode == PeriodComparisonMode.DailyDate ? "year" : "period";
        return $"{count} comparable {Pluralize(noun, count)}";
    }

    private static string CreateComparableSampleLabel(PeriodObservation period)
    {
        return period.ComparisonMode == PeriodComparisonMode.DailyDate
            ? $"comparable {FormatShortDayMonth(period.StartDate)} observations"
            : "comparable periods";
    }

    private static IReadOnlyList<RecentObservationMetricGroupViewModel> BuildMetricGroups(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions,
        string metricGroupLabel)
    {
        var groups = new List<RecentObservationMetricGroupViewModel>();

        // A daily tile is a single day's observation - max/min/mean, not aggregates
        // across days, so it uses its own single group (which also hides the
        // period/daily-extremes toggle, since only one group is present).
        var groupDefinitions = period.Kind == PeriodKind.Daily ? domain.DailyGroups : domain.Groups;

        foreach (var group in groupDefinitions)
        {
            var metrics = new List<RecentObservationMetricViewModel>();
            foreach (var metric in group.Metrics)
            {
                if (period.MetricValues.TryGetValue(metric.Key, out var value))
                {
                    metrics.Add(BuildMetric(metric, value, distributions.GetValueOrDefault(metric.Key)));
                }
            }

            if (metrics.Count > 0)
            {
                groups.Add(new RecentObservationMetricGroupViewModel
                {
                    Key = group.Key,
                    Title = group.Key == "period" ? metricGroupLabel : group.Title,
                    Metrics = metrics,
                });
            }
        }

        return groups;
    }

    private static RecentObservationMetricViewModel BuildMetric(Metric metric, double currentValue, HistoricalValues? distribution)
    {
        var ranking = distribution is null
            ? null
            : RecentObservationComparison.Rank(currentValue, distribution.Values);

        if (ranking is null || distribution is null)
        {
            return new RecentObservationMetricViewModel
            {
                Label = metric.DetailLabel,
                CurrentValue = metric.Format(currentValue),
                ComparablePeriodCount = distribution?.ComparablePeriodCount ?? 0,
                CanShowHistoricalRecord = distribution?.CanShowHistoricalRecord ?? false,
                CanShowHistoricalRange = distribution?.CanShowHistoricalRange ?? false,
                CanShowRank = distribution?.CanShowRank ?? false,
                CanShowPercentile = distribution?.CanShowPercentile ?? false,
            };
        }

        // One rank for the observed value (toward whichever end it is nearer, or a
        // New/Equal record badge at an extreme), plus the record high and record low
        // for the comparison date as plain reference context (no rank of their own).
        var status = RecentObservationComparison.DetermineRecordStatus(ranking);

        return new RecentObservationMetricViewModel
        {
            Label = metric.DetailLabel,
            CurrentValue = metric.Format(currentValue),
            RecordStatus = status,
            RecordStatusText = FormatRecordStatus(status, ranking, distribution),
            RankText = distribution.CanShowRank ? BuildRankText(ranking, status) : null,
            RecordHigh = distribution.CanShowHistoricalRange ? BuildRecordReference(metric, "Record high", ranking.HistoricalMax, distribution.MaxValue) : null,
            RecordLow = distribution.CanShowHistoricalRange ? BuildRecordReference(metric, "Record low", ranking.HistoricalMin, distribution.MinValue) : null,
            ComparablePeriodCount = distribution.ComparablePeriodCount,
            CanShowHistoricalRecord = distribution.CanShowHistoricalRecord,
            CanShowHistoricalRange = distribution.CanShowHistoricalRange,
            CanShowRank = distribution.CanShowRank,
            CanShowPercentile = distribution.CanShowPercentile,
        };
    }

    private static string? BuildRankText(RecentObservationComparisonResult ranking, RecentObservationRecordStatus status)
    {
        // At an extreme the value is shown as a "New record" / "Equal record" badge.
        if (status != RecentObservationRecordStatus.None)
        {
            return null;
        }

        // Otherwise rank toward whichever end the value is nearer (the smaller rank).
        var high = ranking.HighRank <= ranking.LowRank;
        var rank = high ? ranking.HighRank : ranking.LowRank;
        return $"{RecentObservationComparison.FormatOrdinal(rank)} {(high ? "highest" : "lowest")} of {ranking.ComparableCount}";
    }

    private static RecentObservationMetricRecordViewModel BuildRecordReference(
        Metric metric,
        string label,
        double value,
        HistoricalPeriodValue? occurrence)
    {
        return new RecentObservationMetricRecordViewModel
        {
            Label = label,
            Value = metric.Format(value),
            Year = FormatHistoricalOccurrence(occurrence),
        };
    }

    private static string? FormatRecordStatus(
        RecentObservationRecordStatus status,
        RecentObservationComparisonResult ranking,
        HistoricalValues distribution)
    {
        if (status == RecentObservationRecordStatus.None)
        {
            return null;
        }

        if (!distribution.CanShowRank)
        {
            var direction = GetRecordDirectionWord(ranking);
            return status switch
            {
                RecentObservationRecordStatus.NewRecord => $"New {direction} of {ranking.ComparableCount}",
                RecentObservationRecordStatus.EqualRecord => $"Equal {direction} of {ranking.ComparableCount}",
                _ => null,
            };
        }

        return status switch
        {
            RecentObservationRecordStatus.NewRecord => "New record",
            RecentObservationRecordStatus.EqualRecord => "Equal record",
            _ => null,
        };
    }

    private static string GetRecordDirectionWord(RecentObservationComparisonResult ranking)
    {
        if (ranking.IsNewHighRecord || (ranking.IsTiedHighRecord && ranking.HighRank == 1))
        {
            return "high";
        }

        return "low";
    }

    private static RecentObservationTileTone GetTemperatureTone(RecentObservationComparisonResult? ranking)
    {
        return ranking?.Direction switch
        {
            RecentObservationComparisonDirection.High => RecentObservationTileTone.TemperatureWarm,
            RecentObservationComparisonDirection.Low => RecentObservationTileTone.TemperatureCool,
            null => RecentObservationTileTone.Unavailable,
            _ => RecentObservationTileTone.Neutral,
        };
    }

    private static RecentObservationPeriodKind ToTilePeriodKind(PeriodKind periodKind)
    {
        return periodKind switch
        {
            PeriodKind.Daily => RecentObservationPeriodKind.Daily,
            PeriodKind.LatestSevenDays => RecentObservationPeriodKind.LatestSevenDays,
            PeriodKind.CurrentMonth => RecentObservationPeriodKind.CurrentMonth,
            PeriodKind.PreviousMonth => RecentObservationPeriodKind.PreviousMonth,
            PeriodKind.CurrentSeason => RecentObservationPeriodKind.CurrentSeason,
            PeriodKind.PreviousSeason => RecentObservationPeriodKind.PreviousSeason,
            PeriodKind.YearToDate => RecentObservationPeriodKind.YearToDate,
            _ => throw new NotImplementedException(),
        };
    }

    private static RecentObservationTileTone GetPrecipitationTone(RecentObservationComparisonResult? ranking)
    {
        return ranking?.Direction switch
        {
            RecentObservationComparisonDirection.High => RecentObservationTileTone.PrecipitationWet,
            RecentObservationComparisonDirection.Low => RecentObservationTileTone.PrecipitationDry,
            null => RecentObservationTileTone.Unavailable,
            _ => RecentObservationTileTone.Neutral,
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

    private static int GetDayCount(DateOnly startDate, DateOnly endDate)
    {
        return endDate.DayNumber - startDate.DayNumber + 1;
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

    private static int? GetStartYear(IReadOnlyCollection<DailyObservation> records)
    {
        return records.Count == 0 ? null : records.Min(x => x.Date.Year);
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
            PeriodKind.YearToDate => $"{endDate.Year} to date",
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
            return "year to date";
        }

        if (period.StartDate.Day == 1 && period.StartDate.Month == period.EndDate.Month)
        {
            return period.EndDate.Day == DateTime.DaysInMonth(period.EndDate.Year, period.EndDate.Month)
                ? MonthName(period.EndDate.Month)
                : $"{MonthName(period.EndDate.Month)} to date";
        }

        return period.ComparisonLabel;
    }

    private static string CreateMetricGroupLabel(PeriodObservation period)
    {
        return period.Kind switch
        {
            PeriodKind.LatestSevenDays => "7 Days",
            PeriodKind.CurrentMonth or PeriodKind.PreviousMonth => MonthName(period.EndDate.Month),
            PeriodKind.CurrentSeason or PeriodKind.PreviousSeason => period.Title,
            PeriodKind.YearToDate => period.EndDate.Year.ToString(CultureInfo.InvariantCulture),
            _ => period.Title,
        };
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
            PeriodKind.YearToDate => "year to date",
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
            PeriodKind.YearToDate => "year-to-date periods",
            _ => "comparable periods",
        };
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

    private static string FormatDayMonth(DateOnly date)
    {
        return $"{date.Day} {MonthName(date.Month)}";
    }

    private static string FormatShortDayMonth(DateOnly date)
    {
        return date.ToString("d MMM", CultureInfo.CurrentCulture);
    }

    private static string FormatDayMonthYear(DateOnly date)
    {
        return date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
    }

    private static string MonthName(int month)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
    }

    private static string FormatTemperature(double value)
    {
        return $"{value.ToString("0.0", CultureInfo.InvariantCulture)}°C";
    }

    private static string FormatPrecipitation(double value)
    {
        return $"{value.ToString("0.#", CultureInfo.InvariantCulture)}mm";
    }

    private static string FormatAnomaly(double value, Metric metric)
    {
        return $"{(value >= 0 ? "+" : string.Empty)}{metric.Format(value)}";
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

    private static readonly Metric MeanTemperatureMetric = new(
        "temp.mean",
        "Mean temp",
        "Mean temp",
        x => x.Mean,
        MetricAggregation.Mean,
        FormatTemperature,
        "Mean temperature");

    private static readonly Metric AverageMaxTemperatureMetric = new(
        "temp.max",
        "Max temp",
        "Average max temp",
        x => x.Max,
        MetricAggregation.Mean,
        FormatTemperature,
        "Average maximum temperature");

    private static readonly Metric AverageMinTemperatureMetric = new(
        "temp.min",
        "Min temp",
        "Average min temp",
        x => x.Min,
        MetricAggregation.Mean,
        FormatTemperature,
        "Average minimum temperature");

    private static readonly Metric HighestDailyMaxTemperatureMetric = new(
        "temp.daily-max-high",
        "Highest daily max",
        "Highest daily max",
        x => x.Max,
        MetricAggregation.Max,
        FormatTemperature,
        "Highest daily maximum");

    private static readonly Metric LowestDailyMaxTemperatureMetric = new(
        "temp.daily-max-low",
        "Lowest daily max",
        "Lowest daily max",
        x => x.Max,
        MetricAggregation.Min,
        FormatTemperature,
        "Lowest daily maximum");

    private static readonly Metric HighestDailyMinTemperatureMetric = new(
        "temp.daily-min-high",
        "Highest daily min",
        "Highest daily min",
        x => x.Min,
        MetricAggregation.Max,
        FormatTemperature,
        "Highest daily minimum");

    private static readonly Metric LowestDailyMinTemperatureMetric = new(
        "temp.daily-min-low",
        "Lowest daily min",
        "Lowest daily min",
        x => x.Min,
        MetricAggregation.Min,
        FormatTemperature,
        "Lowest daily minimum");

    // Daily tiles describe a single day, which has a maximum, a minimum and a mean
    // — not aggregates across days. These reuse the period-metric keys (so their
    // historical distributions are computed once, by calendar date) but carry
    // single-day labels for the expanded view.
    private static readonly Metric DailyMaxTemperatureMetric = new(
        "temp.daily-max-high",
        "Maximum",
        "Maximum",
        x => x.Max,
        MetricAggregation.Max,
        FormatTemperature,
        "Maximum");

    private static readonly Metric DailyMinTemperatureMetric = new(
        "temp.daily-min-low",
        "Minimum",
        "Minimum",
        x => x.Min,
        MetricAggregation.Min,
        FormatTemperature,
        "Minimum");

    private static readonly Metric DailyMeanTemperatureMetric = new(
        "temp.mean",
        "Mean",
        "Mean",
        x => x.Mean,
        MetricAggregation.Mean,
        FormatTemperature,
        "Mean");

    private static readonly Metric PrecipitationMetric = new(
        "precip.total",
        "Precipitation total",
        "Precipitation total",
        x => x.Precipitation,
        MetricAggregation.Sum,
        FormatPrecipitation,
        "Total precipitation");

    private static readonly Metric HighestDailyPrecipitationMetric = new(
        "precip.daily-high",
        "Highest daily precipitation",
        "Highest daily precipitation",
        x => x.Precipitation,
        MetricAggregation.Max,
        FormatPrecipitation,
        "Highest daily precipitation");

    private static readonly Metric DailyPrecipitationMetric = new(
        "precip.total",
        "Precipitation",
        "Precipitation",
        x => x.Precipitation,
        MetricAggregation.Sum,
        FormatPrecipitation,
        "Precipitation");

    private static readonly MetricDomain TemperatureDomain = new(
        MeanTemperatureMetric,
        [AverageMaxTemperatureMetric, AverageMinTemperatureMetric],
        [
            new MetricGroup("period", "Period", [AverageMaxTemperatureMetric, AverageMinTemperatureMetric, MeanTemperatureMetric]),
            new MetricGroup("daily-extremes", "Daily extremes", [HighestDailyMaxTemperatureMetric, LowestDailyMaxTemperatureMetric, HighestDailyMinTemperatureMetric, LowestDailyMinTemperatureMetric]),
        ],
        [
            new MetricGroup("day", "Day", [DailyMaxTemperatureMetric, DailyMinTemperatureMetric, DailyMeanTemperatureMetric]),
        ],
        "Mean temperature",
        ShowHistoricalMin: true,
        "Warmest",
        "Coolest",
        RecentObservationComparison.BuildTemperatureHeadline,
        RecentObservationComparison.BuildTemperaturePercentileSentence,
        GetTemperatureTone);

    private static readonly MetricDomain PrecipitationDomain = new(
        PrecipitationMetric,
        [],
        [
            new MetricGroup("period", "Period", [PrecipitationMetric]),
            new MetricGroup("daily-extremes", "Daily extremes", [HighestDailyPrecipitationMetric]),
        ],
        [
            new MetricGroup("day", "Day", [DailyPrecipitationMetric]),
        ],
        "Precipitation total",
        ShowHistoricalMin: true,
        "Wettest",
        "Driest",
        RecentObservationComparison.BuildPrecipitationHeadline,
        (_, startYear, ranking) => RecentObservationComparison.BuildPrecipitationPercentileSentence(startYear, ranking),
        GetPrecipitationTone);

    private sealed record DailyObservation(DateOnly Date, double? Max, double? Min, double? Mean, double? Precipitation);

    private sealed record ReferenceDateResolution(
        DateOnly? ReferenceDate,
        DateOnly? MinimumReferenceDate,
        DateOnly? MaximumReferenceDate);

    private enum MetricAggregation
    {
        Mean,
        Sum,
        Max,
        Min,
    }

    private sealed record Metric(
        string Key,
        string SingularLabel,
        string PluralLabel,
        Func<DailyObservation, double?> Select,
        MetricAggregation Aggregation,
        Func<double, string> Format,
        string DetailLabel);

    private sealed record MetricGroup(string Key, string Title, IReadOnlyList<Metric> Metrics);

    private sealed record MetricDomain(
        Metric Primary,
        IReadOnlyList<Metric> Supporting,
        IReadOnlyList<MetricGroup> Groups,
        IReadOnlyList<MetricGroup> DailyGroups,
        string PrimaryLabel,
        bool ShowHistoricalMin,
        string HistoricalMaxWord,
        string HistoricalMinWord,
        Func<string, RecentObservationComparisonResult, string> BuildHeadline,
        Func<string, int?, RecentObservationComparisonResult, string> BuildPercentileSentence,
        Func<RecentObservationComparisonResult?, RecentObservationTileTone> GetTone)
    {
        public IReadOnlyList<Metric> AllMetrics
        {
            get
            {
                var seen = new HashSet<string>();
                var result = new List<Metric>();
                var all = new[] { Primary }
                    .Concat(Supporting)
                    .Concat(Groups.SelectMany(x => x.Metrics))
                    .Concat(DailyGroups.SelectMany(x => x.Metrics));
                foreach (var metric in all)
                {
                    if (seen.Add(metric.Key))
                    {
                        result.Add(metric);
                    }
                }

                return result;
            }
        }
    }

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
        IReadOnlyDictionary<string, double> MetricValues);

    private sealed record ObservationCompleteness(int AvailableObservationCount, int ExpectedObservationCount)
    {
        public static ObservationCompleteness CompleteDay { get; } = new(1, 1);
    }

    private sealed record PreviousMonthPeriod(DateOnly StartDate, DateOnly EndDate, int Offset);

    private sealed record PreviousDayPeriod<TRecord>(TRecord Record, string Title, int Offset);

    private sealed record HistoricalValues(
        List<HistoricalPeriodValue> PeriodValues,
        int? StartYear,
        string? UnavailableReason,
        int MinimumRankSampleSize)
    {
        public List<double?> Values => [.. PeriodValues.Select(x => x.Value)];

        public int ComparablePeriodCount => PeriodValues.Count(x => x.Value.HasValue && double.IsFinite(x.Value.Value));

        public bool CanShowHistoricalRecord => ComparablePeriodCount >= 1;

        public bool CanShowHistoricalRange => ComparablePeriodCount >= 2;

        public bool CanShowRank => ComparablePeriodCount >= MinimumRankSampleSize;

        public bool CanShowPercentile => CanShowRank;

        public HistoricalPeriodValue? MaxValue => PeriodValues
            .Where(x => x.Value.HasValue && double.IsFinite(x.Value.Value))
            .OrderByDescending(x => x.Value!.Value)
            .ThenBy(x => x.Year)
            .FirstOrDefault();

        public HistoricalPeriodValue? MinValue => PeriodValues
            .Where(x => x.Value.HasValue && double.IsFinite(x.Value.Value))
            .OrderBy(x => x.Value!.Value)
            .ThenBy(x => x.Year)
            .FirstOrDefault();
    }

    private sealed record HistoricalPeriodValue(double? Value, short? Year);

    private enum PeriodKind
    {
        Daily,
        LatestSevenDays,
        CurrentMonth,
        PreviousMonth,
        CurrentSeason,
        PreviousSeason,
        YearToDate,
    }

    private enum PeriodComparisonMode
    {
        DailyDate,
        DailyRange,
    }
}
#pragma warning restore SA1201, SA1204
