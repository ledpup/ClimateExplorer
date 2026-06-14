#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.WebApiClient.Services;
using static ClimateExplorer.Core.Enums;

public sealed class RecentObservationsService : IRecentObservationsService
{
    private const int MinimumHistoricalPeriods = 20;
    private const double MinimumHistoricalCoverage = 0.9d;
    private const int MaximumPreviousMonthCount = 11;
    private const int MaximumPreviousSeasonCount = 3;

    private readonly IDataService dataService;

    public RecentObservationsService(IDataService dataService)
    {
        this.dataService = dataService;
    }

    public async Task<RecentObservationsTabResult> GetTemperatureRecords(Location location, int previousMonthCount, int previousSeasonCount)
    {
        previousMonthCount = Math.Clamp(previousMonthCount, 0, MaximumPreviousMonthCount);
        previousSeasonCount = Math.Clamp(previousSeasonCount, 0, MaximumPreviousSeasonCount);

        var locationId = location.Id;
        var maxTask = dataService.GetRecentObservations(locationId, DataType.TempMax);
        var minTask = dataService.GetRecentObservations(locationId, DataType.TempMin);

        await Task.WhenAll(maxTask, minTask);

        var maxResponse = await maxTask;
        var minResponse = await minTask;

        if (!maxResponse.IsSupported || !minResponse.IsSupported)
        {
            return new RecentObservationsTabResult
            {
                IsSupported = false,
                EmptyMessage = "Recent temperature observations are not available for this location.",
            };
        }

        var latestDaily = BuildDailyTemperature(maxResponse.Records, minResponse.Records);
        if (latestDaily.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = "No recent temperature observations are available yet.",
            };
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var periods = BuildTemperaturePeriods(latestDaily, today, location.Coordinates.Latitude, previousMonthCount, previousSeasonCount);
        if (periods.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = "No recent temperature observation periods can be calculated yet.",
            };
        }

        var dailyHistory = await GetTemperatureHistoricalDailyRecords(locationId, DataAdjustment.Unadjusted);
        var sharedMonthlyHistoryTask = ShouldUseSharedMonthlyHistory(periods)
            ? GetTemperatureHistoricalMonthlyRecords(locationId, DataAdjustment.Unadjusted, null)
            : null;
        var tiles = new List<RecentObservationTileViewModel>();

        foreach (var period in periods)
        {
            var historicalValues = period.ComparisonMode == PeriodComparisonMode.MonthlySameMonth
                ? await GetTemperatureHistoricalMonthlyValues(locationId, DataAdjustment.Unadjusted, dailyHistory, period, sharedMonthlyHistoryTask)
                : GetTemperatureHistoricalDailyValues(dailyHistory, period);

            tiles.Add(BuildTemperatureTile(period, historicalValues));
        }

        return new RecentObservationsTabResult
        {
            EmptyMessage = "No recent temperature observations are available.",
            Tiles = tiles,
        };
    }

    public async Task<RecentObservationsTabResult> GetPrecipitationRecords(Location location, int previousMonthCount, int previousSeasonCount)
    {
        previousMonthCount = Math.Clamp(previousMonthCount, 0, MaximumPreviousMonthCount);
        previousSeasonCount = Math.Clamp(previousSeasonCount, 0, MaximumPreviousSeasonCount);

        var locationId = location.Id;
        var latestResponse = await dataService.GetRecentObservations(locationId, DataType.Precipitation);

        if (!latestResponse.IsSupported)
        {
            return new RecentObservationsTabResult
            {
                IsSupported = false,
                EmptyMessage = "Recent precipitation observations are not available for this location.",
            };
        }

        var latestDaily = BuildDailyPrecipitation(latestResponse.Records);
        if (latestDaily.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = "No recent precipitation observations are available yet.",
            };
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var periods = BuildPrecipitationPeriods(latestDaily, today, location.Coordinates.Latitude, previousMonthCount, previousSeasonCount);
        if (periods.Count == 0)
        {
            return new RecentObservationsTabResult
            {
                EmptyMessage = "No recent precipitation observation periods can be calculated yet.",
            };
        }

        var dailyHistory = await GetHistoricalRecords(locationId, DataType.Precipitation, null, monthly: false);
        var sharedMonthlyHistoryTask = ShouldUseSharedMonthlyHistory(periods)
            ? GetHistoricalRecords(locationId, DataType.Precipitation, null, monthly: true)
            : null;
        var tiles = new List<RecentObservationTileViewModel>();

        foreach (var period in periods)
        {
            var historicalValues = period.ComparisonMode == PeriodComparisonMode.MonthlySameMonth
                ? await GetPrecipitationHistoricalMonthlyValues(locationId, dailyHistory, period, sharedMonthlyHistoryTask)
                : GetPrecipitationHistoricalDailyValues(dailyHistory, period);

            tiles.Add(BuildPrecipitationTile(period, historicalValues));
        }

        return new RecentObservationsTabResult
        {
            EmptyMessage = "No recent precipitation observations are available.",
            Tiles = tiles,
        };
    }

    private static List<DailyTemperature> BuildDailyTemperature(IEnumerable<DataRecord> maxRecords, IEnumerable<DataRecord> minRecords)
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
                return new DailyTemperature(date, (max + min) / 2d, max, min);
            })
            .OrderBy(x => x.Date)];
    }

    private static List<DailyPrecipitation> BuildDailyPrecipitation(IEnumerable<DataRecord> records)
    {
        return [.. records
            .Where(x => x.Date.HasValue && x.Value.HasValue)
            .Select(x => new DailyPrecipitation(x.Date!.Value, x.Value!.Value))
            .OrderBy(x => x.Date)];
    }

    private static List<TRecord> GetRecordsInRange<TRecord>(
        IEnumerable<TRecord> records,
        Func<TRecord, DateOnly> getDate,
        DateOnly startDate,
        DateOnly endDate)
    {
        return [.. records.Where(x =>
        {
            var date = getDate(x);
            return date >= startDate && date <= endDate;
        })];
    }

    private static List<PeriodObservation> BuildTemperaturePeriods(
        List<DailyTemperature> daily,
        DateOnly today,
        double latitude,
        int previousMonthCount,
        int previousSeasonCount)
    {
        var periods = new List<PeriodObservation>();
        var byDate = daily.ToDictionary(x => x.Date);

        if (byDate.TryGetValue(today, out var todayRecord))
        {
            periods.Add(CreateTemperatureDailyPeriod("Today", todayRecord));
        }

        var yesterday = today.AddDays(-1);
        if (byDate.TryGetValue(yesterday, out var yesterdayRecord))
        {
            periods.Add(CreateTemperatureDailyPeriod("Yesterday", yesterdayRecord));
        }

        var latestDate = daily.Max(x => x.Date);
        var lastWeekEnd = latestDate;
        var lastWeekStart = latestDate.AddDays(-6);
        var lastWeekRecords = GetRecordsInRange(daily, x => x.Date, lastWeekStart, lastWeekEnd);
        AddTemperatureRangePeriod(periods, lastWeekRecords, lastWeekStart, lastWeekEnd, PeriodKind.LastWeek);

        if (latestDate.Year == today.Year && latestDate.Month == today.Month)
        {
            var monthStart = new DateOnly(latestDate.Year, latestDate.Month, 1);
            var monthRecords = GetRecordsInRange(daily, x => x.Date, monthStart, latestDate);
            AddTemperatureRangePeriod(periods, monthRecords, monthStart, latestDate, PeriodKind.CurrentMonth);
        }

        foreach (var previousMonth in GetPreviousMonthPeriods(today, previousMonthCount))
        {
            var previousMonthRecords = GetRecordsInRange(daily, x => x.Date, previousMonth.StartDate, previousMonth.EndDate);
            AddTemperatureRangePeriod(
                periods,
                previousMonthRecords,
                previousMonth.StartDate,
                previousMonth.EndDate,
                PeriodKind.PreviousMonth,
                previousMonth.Offset);
        }

        var currentSeason = MeteorologicalSeasonCalculator.GetCurrentSeason(today, latitude);
        if (latestDate >= currentSeason.StartDate && latestDate <= currentSeason.EndDate)
        {
            var currentSeasonToDate = currentSeason with { EndDate = latestDate };
            var currentSeasonRecords = GetRecordsInRange(daily, x => x.Date, currentSeasonToDate.StartDate, currentSeasonToDate.EndDate);
            AddTemperatureRangePeriod(
                periods,
                currentSeasonRecords,
                currentSeasonToDate.StartDate,
                currentSeasonToDate.EndDate,
                PeriodKind.CurrentSeason,
                seasonPeriod: currentSeasonToDate,
                isSeasonToDate: true);
        }

        var previousSeasons = MeteorologicalSeasonCalculator.GetPreviousSeasons(today, latitude, previousSeasonCount);
        for (var index = 0; index < previousSeasons.Count; index++)
        {
            var previousSeason = previousSeasons[index];
            var previousSeasonRecords = GetRecordsInRange(daily, x => x.Date, previousSeason.StartDate, previousSeason.EndDate);
            AddTemperatureRangePeriod(
                periods,
                previousSeasonRecords,
                previousSeason.StartDate,
                previousSeason.EndDate,
                PeriodKind.PreviousSeason,
                seasonPeriod: previousSeason,
                periodOffset: index + 1);
        }

        if (latestDate.Year == today.Year)
        {
            var ytdStart = new DateOnly(latestDate.Year, 1, 1);
            var ytdRecords = GetRecordsInRange(daily, x => x.Date, ytdStart, latestDate);
            AddTemperatureRangePeriod(periods, ytdRecords, ytdStart, latestDate, PeriodKind.YearToDate);
        }

        return periods;
    }

    private static List<PeriodObservation> BuildPrecipitationPeriods(
        List<DailyPrecipitation> daily,
        DateOnly today,
        double latitude,
        int previousMonthCount,
        int previousSeasonCount)
    {
        var periods = new List<PeriodObservation>();
        var byDate = daily.ToDictionary(x => x.Date);

        if (byDate.TryGetValue(today, out var todayRecord))
        {
            periods.Add(CreatePrecipitationDailyPeriod("Today", todayRecord));
        }

        var yesterday = today.AddDays(-1);
        if (byDate.TryGetValue(yesterday, out var yesterdayRecord))
        {
            periods.Add(CreatePrecipitationDailyPeriod("Yesterday", yesterdayRecord));
        }

        var latestDate = daily.Max(x => x.Date);
        var lastWeekEnd = latestDate;
        var lastWeekStart = latestDate.AddDays(-6);
        var lastWeekRecords = GetRecordsInRange(daily, x => x.Date, lastWeekStart, lastWeekEnd);
        AddPrecipitationRangePeriod(periods, lastWeekRecords, lastWeekStart, lastWeekEnd, PeriodKind.LastWeek);

        if (latestDate.Year == today.Year && latestDate.Month == today.Month)
        {
            var monthStart = new DateOnly(latestDate.Year, latestDate.Month, 1);
            var monthRecords = GetRecordsInRange(daily, x => x.Date, monthStart, latestDate);
            AddPrecipitationRangePeriod(periods, monthRecords, monthStart, latestDate, PeriodKind.CurrentMonth);
        }

        foreach (var previousMonth in GetPreviousMonthPeriods(today, previousMonthCount))
        {
            var previousMonthRecords = GetRecordsInRange(daily, x => x.Date, previousMonth.StartDate, previousMonth.EndDate);
            AddPrecipitationRangePeriod(
                periods,
                previousMonthRecords,
                previousMonth.StartDate,
                previousMonth.EndDate,
                PeriodKind.PreviousMonth,
                previousMonth.Offset);
        }

        var currentSeason = MeteorologicalSeasonCalculator.GetCurrentSeason(today, latitude);
        if (latestDate >= currentSeason.StartDate && latestDate <= currentSeason.EndDate)
        {
            var currentSeasonToDate = currentSeason with { EndDate = latestDate };
            var currentSeasonRecords = GetRecordsInRange(daily, x => x.Date, currentSeasonToDate.StartDate, currentSeasonToDate.EndDate);
            AddPrecipitationRangePeriod(
                periods,
                currentSeasonRecords,
                currentSeasonToDate.StartDate,
                currentSeasonToDate.EndDate,
                PeriodKind.CurrentSeason,
                seasonPeriod: currentSeasonToDate,
                isSeasonToDate: true);
        }

        var previousSeasons = MeteorologicalSeasonCalculator.GetPreviousSeasons(today, latitude, previousSeasonCount);
        for (var index = 0; index < previousSeasons.Count; index++)
        {
            var previousSeason = previousSeasons[index];
            var previousSeasonRecords = GetRecordsInRange(daily, x => x.Date, previousSeason.StartDate, previousSeason.EndDate);
            AddPrecipitationRangePeriod(
                periods,
                previousSeasonRecords,
                previousSeason.StartDate,
                previousSeason.EndDate,
                PeriodKind.PreviousSeason,
                seasonPeriod: previousSeason,
                periodOffset: index + 1);
        }

        if (latestDate.Year == today.Year)
        {
            var ytdStart = new DateOnly(latestDate.Year, 1, 1);
            var ytdRecords = GetRecordsInRange(daily, x => x.Date, ytdStart, latestDate);
            AddPrecipitationRangePeriod(periods, ytdRecords, ytdStart, latestDate, PeriodKind.YearToDate);
        }

        return periods;
    }

    private static PeriodObservation CreateTemperatureDailyPeriod(string title, DailyTemperature record)
    {
        return new PeriodObservation(
            title,
            FormatDayMonth(record.Date),
            $"{FormatDayMonth(record.Date)} days",
            record.Date,
            record.Date,
            record.Mean,
            record.Max,
            record.Min,
            1,
            1,
            true,
            PeriodKind.Daily,
            PeriodComparisonMode.DailyDate,
            null,
            null);
    }

    private static PeriodObservation CreatePrecipitationDailyPeriod(string title, DailyPrecipitation record)
    {
        return new PeriodObservation(
            title,
            FormatDayMonth(record.Date),
            $"{FormatDayMonth(record.Date)} days",
            record.Date,
            record.Date,
            record.Rainfall,
            null,
            null,
            1,
            1,
            true,
            PeriodKind.Daily,
            PeriodComparisonMode.DailyDate,
            null,
            null);
    }

    private static void AddTemperatureRangePeriod(
        List<PeriodObservation> periods,
        List<DailyTemperature> records,
        DateOnly startDate,
        DateOnly endDate,
        PeriodKind kind,
        int? previousMonthOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false,
        int? periodOffset = null)
    {
        if (records.Count == 0)
        {
            return;
        }

        var expectedDays = GetDayCount(startDate, endDate);
        var actualDays = records.Select(x => x.Date).Distinct().Count();
        var canCompare = actualDays == expectedDays;
        var note = CreatePeriodNote(kind, endDate, actualDays, expectedDays, canCompare);

        periods.Add(new PeriodObservation(
            CreatePeriodTitle(kind, startDate, endDate, previousMonthOffset, seasonPeriod, isSeasonToDate),
            CreateComparisonLabel(kind, endDate, seasonPeriod, isSeasonToDate),
            CreateComparisonLabelPlural(kind, endDate, seasonPeriod, isSeasonToDate),
            startDate,
            endDate,
            records.Average(x => x.Mean),
            records.Average(x => x.Max),
            records.Average(x => x.Min),
            actualDays,
            expectedDays,
            canCompare,
            kind,
            PeriodComparisonMode.DailyRange,
            periodOffset ?? previousMonthOffset,
            note));
    }

    private static void AddPrecipitationRangePeriod(
        List<PeriodObservation> periods,
        List<DailyPrecipitation> records,
        DateOnly startDate,
        DateOnly endDate,
        PeriodKind kind,
        int? previousMonthOffset = null,
        MeteorologicalSeasonPeriod? seasonPeriod = null,
        bool isSeasonToDate = false,
        int? periodOffset = null)
    {
        if (records.Count == 0)
        {
            return;
        }

        var expectedDays = GetDayCount(startDate, endDate);
        var actualDays = records.Select(x => x.Date).Distinct().Count();
        var canCompare = actualDays == expectedDays;
        var note = CreatePeriodNote(kind, endDate, actualDays, expectedDays, canCompare);

        periods.Add(new PeriodObservation(
            CreatePeriodTitle(kind, startDate, endDate, previousMonthOffset, seasonPeriod, isSeasonToDate),
            CreateComparisonLabel(kind, endDate, seasonPeriod, isSeasonToDate),
            CreateComparisonLabelPlural(kind, endDate, seasonPeriod, isSeasonToDate),
            startDate,
            endDate,
            records.Sum(x => x.Rainfall),
            null,
            null,
            actualDays,
            expectedDays,
            canCompare,
            kind,
            PeriodComparisonMode.DailyRange,
            periodOffset ?? previousMonthOffset,
            note));
    }

    private async Task<HistoricalValues> GetTemperatureHistoricalMonthlyValues(
        Guid locationId,
        DataAdjustment? preferredAdjustment,
        ClimateRecordsResponse dailyHistory,
        PeriodObservation period,
        Task<ClimateRecordsResponse>? sharedMonthlyHistoryTask)
    {
        if (!period.CanCompare)
        {
            return HistoricalValues.Unavailable("Recent daily observations are incomplete for this period.");
        }

        var monthlyHistory = sharedMonthlyHistoryTask is null
            ? await GetTemperatureHistoricalMonthlyRecords(locationId, preferredAdjustment, period.StartDate.Month)
            : await sharedMonthlyHistoryTask;
        var monthlyValues = monthlyHistory.Records
            .Where(x => x.Value.HasValue && x.Month == period.StartDate.Month && x.Year != period.StartDate.Year)
            .Select(x => new HistoricalPeriodValue(x.Value, x.Year))
            .ToList();

        if (monthlyValues.Count >= MinimumHistoricalPeriods)
        {
            return new HistoricalValues(monthlyValues, monthlyHistory.StartYear, null);
        }

        return GetTemperatureHistoricalDailyValues(dailyHistory, period);
    }

    private async Task<HistoricalValues> GetPrecipitationHistoricalMonthlyValues(
        Guid locationId,
        ClimateRecordsResponse dailyHistory,
        PeriodObservation period,
        Task<ClimateRecordsResponse>? sharedMonthlyHistoryTask)
    {
        if (!period.CanCompare)
        {
            return HistoricalValues.Unavailable("Recent daily observations are incomplete for this period.");
        }

        var monthlyHistory = sharedMonthlyHistoryTask is null
            ? await GetHistoricalRecords(locationId, DataType.Precipitation, null, monthly: true, month: period.StartDate.Month)
            : await sharedMonthlyHistoryTask;
        var monthlyValues = monthlyHistory.Records
            .Where(x => x.Value.HasValue && x.Month == period.StartDate.Month && x.Year != period.StartDate.Year)
            .Select(x => new HistoricalPeriodValue(x.Value, x.Year))
            .ToList();

        if (monthlyValues.Count >= MinimumHistoricalPeriods)
        {
            return new HistoricalValues(monthlyValues, monthlyHistory.StartYear, null);
        }

        return GetPrecipitationHistoricalDailyValues(dailyHistory, period);
    }

    private static HistoricalValues GetTemperatureHistoricalDailyValues(ClimateRecordsResponse response, PeriodObservation period)
    {
        if (!period.CanCompare)
        {
            return HistoricalValues.Unavailable("Recent daily observations are incomplete for this period.");
        }

        if (period.ComparisonMode == PeriodComparisonMode.DailyDate)
        {
            return GetHistoricalDailyDateValues(response, period);
        }

        return GetHistoricalDailyRangeValues(response, period, values => values.Average());
    }

    private static HistoricalValues GetPrecipitationHistoricalDailyValues(ClimateRecordsResponse response, PeriodObservation period)
    {
        if (!period.CanCompare)
        {
            return HistoricalValues.Unavailable("Recent daily observations are incomplete for this period.");
        }

        if (period.ComparisonMode == PeriodComparisonMode.DailyDate)
        {
            return GetHistoricalDailyDateValues(response, period);
        }

        return GetHistoricalDailyRangeValues(response, period, values => values.Sum());
    }

    private static HistoricalValues GetHistoricalDailyDateValues(ClimateRecordsResponse response, PeriodObservation period)
    {
        var values = response.Records
            .Where(x => x.Value.HasValue &&
                        x.Month == period.StartDate.Month &&
                        x.Day == period.StartDate.Day &&
                        x.Year != period.StartDate.Year)
            .Select(x => new HistoricalPeriodValue(x.Value, x.Year))
            .ToList();

        return values.Count >= MinimumHistoricalPeriods
            ? new HistoricalValues(values, response.StartYear, null)
            : HistoricalValues.Unavailable("Not enough historical daily records are available for this date.");
    }

    private static HistoricalValues GetHistoricalDailyRangeValues(
        ClimateRecordsResponse response,
        PeriodObservation period,
        Func<List<double>, double> aggregate)
    {
        var values = response.Records
            .Where(x => x.Date.HasValue &&
                        x.Value.HasValue &&
                        IsWithinEquivalentRange(x.Date.Value, period.StartDate, period.EndDate))
            .Select(x => new
            {
                Record = x,
                EquivalentPeriodYear = GetEquivalentPeriodYear(x.Date!.Value, period.StartDate, period.EndDate),
            })
            .Where(x => x.EquivalentPeriodYear != period.StartDate.Year)
            .GroupBy(x => x.EquivalentPeriodYear)
            .Select(group =>
            {
                var expectedDays = GetEquivalentDayCount(group.Key, period.StartDate, period.EndDate);
                var values = group
                    .Select(x => x.Record.Value!.Value)
                    .ToList();
                var requiredDays = (int)Math.Ceiling(expectedDays * MinimumHistoricalCoverage);

                return values.Count >= requiredDays
                    ? (Value: (double?)aggregate(values), Year: (short?)group.Key, Count: values.Count)
                    : (Value: null, Year: (short?)null, Count: values.Count);
            })
            .Where(x => x.Value.HasValue)
            .Select(x => new HistoricalPeriodValue(x.Value, x.Year))
            .ToList();

        return values.Count >= MinimumHistoricalPeriods
            ? new HistoricalValues(values, response.StartYear, null)
            : HistoricalValues.Unavailable("Not enough equivalent historical daily periods are available.");
    }

    private async Task<ClimateRecordsResponse> GetTemperatureHistoricalDailyRecords(Guid locationId, DataAdjustment? preferredAdjustment)
    {
        var mean = await GetHistoricalRecords(locationId, DataType.TempMean, preferredAdjustment, monthly: false);
        if (mean.Records.Count > 0)
        {
            return mean;
        }

        var maxTask = GetHistoricalRecords(locationId, DataType.TempMax, preferredAdjustment, monthly: false);
        var minTask = GetHistoricalRecords(locationId, DataType.TempMin, preferredAdjustment, monthly: false);
        await Task.WhenAll(maxTask, minTask);

        return CombineTemperatureRecords(await maxTask, await minTask, DataResolution.Daily);
    }

    private async Task<ClimateRecordsResponse> GetTemperatureHistoricalMonthlyRecords(Guid locationId, DataAdjustment? preferredAdjustment, int? month)
    {
        var mean = await GetHistoricalRecords(locationId, DataType.TempMean, preferredAdjustment, monthly: true, month: month);
        if (mean.Records.Count > 0)
        {
            return mean;
        }

        var maxTask = GetHistoricalRecords(locationId, DataType.TempMax, preferredAdjustment, monthly: true, month: month);
        var minTask = GetHistoricalRecords(locationId, DataType.TempMin, preferredAdjustment, monthly: true, month: month);
        await Task.WhenAll(maxTask, minTask);

        return CombineTemperatureRecords(await maxTask, await minTask, DataResolution.Monthly);
    }

    private async Task<ClimateRecordsResponse> GetHistoricalRecords(
        Guid locationId,
        DataType dataType,
        DataAdjustment? preferredAdjustment,
        bool monthly,
        int? month = null)
    {
        foreach (var adjustment in GetAdjustmentCandidates(dataType, preferredAdjustment))
        {
            var response = await dataService.GetClimateRecords(locationId, dataType, adjustment, month: month, monthly: monthly);
            if (response.Records.Count > 0)
            {
                return response;
            }
        }

        return new ClimateRecordsResponse
        {
            DataType = dataType,
            DataAdjustment = preferredAdjustment,
            DataResolution = monthly ? DataResolution.Monthly : DataResolution.Daily,
        };
    }

    private static ClimateRecordsResponse CombineTemperatureRecords(
        ClimateRecordsResponse maxResponse,
        ClimateRecordsResponse minResponse,
        DataResolution dataResolution)
    {
        var minByKey = minResponse.Records
            .Where(x => x.Value.HasValue && x.Key is not null)
            .ToDictionary(x => x.Key!, x => x.Value!.Value);

        var records = maxResponse.Records
            .Where(x => x.Value.HasValue && x.Key is not null && minByKey.ContainsKey(x.Key!))
            .Select(x => new DataRecord(x.Year, x.Month, x.Day, (x.Value!.Value + minByKey[x.Key!]) / 2d))
            .ToList();

        return new ClimateRecordsResponse
        {
            Records = records,
            DataAdjustment = maxResponse.DataAdjustment ?? minResponse.DataAdjustment,
            DataResolution = dataResolution,
            DataType = DataType.TempMean,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
            StartYear = GetStartYear(records),
            EndYear = GetEndYear(records),
            TotalCount = records.Count,
        };
    }

    private static RecentObservationTileViewModel BuildTemperatureTile(PeriodObservation period, HistoricalValues historicalValues)
    {
        var ranking = RecentObservationComparison.Rank(period.PrimaryValue, historicalValues.Values);
        var stats = new List<RecentObservationStatViewModel>
        {
            new() { Label = period.ActualDays == 1 ? "Max temp" : "Average max temp", Value = FormatTemperature(period.SupportingValueOne!.Value) },
            new() { Label = period.ActualDays == 1 ? "Min temp" : "Average min temp", Value = FormatTemperature(period.SupportingValueTwo!.Value) },
        };

        if (ranking is not null)
        {
            stats.Add(new RecentObservationStatViewModel { Label = "Historical average", Value = FormatTemperature(ranking.HistoricalAverage) });
            stats.Add(new RecentObservationStatViewModel { Label = "Anomaly", Value = FormatTemperatureAnomaly(ranking.Anomaly) });
        }

        return new RecentObservationTileViewModel
        {
            PeriodKind = ToTilePeriodKind(period.Kind),
            PeriodOffset = period.PeriodOffset,
            PeriodTitle = period.Title,
            Headline = ranking is null ? "Comparison unavailable" : RecentObservationComparison.BuildTemperatureHeadline(period.ComparisonLabel, ranking),
            PercentileSentence = ranking is null
                ? historicalValues.UnavailableReason ?? "Not enough historical data is available for this comparison."
                : RecentObservationComparison.BuildTemperaturePercentileSentence(period.ComparisonLabelPlural, historicalValues.StartYear, ranking),
            PrimaryLabel = "Mean temperature",
            PrimaryValue = FormatTemperature(period.PrimaryValue),
            HistoricalMaxLabel = ranking is null ? null : $"Warmest {CreateHistoricalContextLabel(period)}",
            HistoricalMaxValue = ranking is null ? null : FormatTemperature(ranking.HistoricalMax),
            HistoricalMaxOccurred = FormatHistoricalOccurrence(historicalValues.MaxValue),
            HistoricalMinLabel = ranking is null ? null : $"Coolest {CreateHistoricalContextLabel(period)}",
            HistoricalMinValue = ranking is null ? null : FormatTemperature(ranking.HistoricalMin),
            HistoricalMinOccurred = FormatHistoricalOccurrence(historicalValues.MinValue),
            HasComparison = ranking is not null,
            Tone = GetTemperatureTone(ranking),
            Note = period.Note,
            Stats = stats,
        };
    }

    private static RecentObservationTileViewModel BuildPrecipitationTile(PeriodObservation period, HistoricalValues historicalValues)
    {
        var ranking = RecentObservationComparison.Rank(period.PrimaryValue, historicalValues.Values);
        var stats = new List<RecentObservationStatViewModel>();

        if (ranking is not null)
        {
            stats.Add(new RecentObservationStatViewModel { Label = "Historical average", Value = FormatRainfall(ranking.HistoricalAverage) });
            stats.Add(new RecentObservationStatViewModel { Label = "Anomaly", Value = FormatRainfallAnomaly(ranking.Anomaly) });
        }

        return new RecentObservationTileViewModel
        {
            PeriodKind = ToTilePeriodKind(period.Kind),
            PeriodOffset = period.PeriodOffset,
            PeriodTitle = period.Title,
            Headline = ranking is null ? "Comparison unavailable" : RecentObservationComparison.BuildPrecipitationHeadline(period.ComparisonLabel, ranking),
            PercentileSentence = ranking is null
                ? historicalValues.UnavailableReason ?? "Not enough historical data is available for this comparison."
                : RecentObservationComparison.BuildPrecipitationPercentileSentence(historicalValues.StartYear, ranking),
            PrimaryLabel = "Rainfall total",
            PrimaryValue = FormatRainfall(period.PrimaryValue),
            HistoricalMaxLabel = ranking is null ? null : $"Wettest {CreateHistoricalContextLabel(period)}",
            HistoricalMaxValue = ranking is null ? null : FormatRainfall(ranking.HistoricalMax),
            HistoricalMaxOccurred = FormatHistoricalOccurrence(historicalValues.MaxValue),
            HasComparison = ranking is not null,
            Tone = GetPrecipitationTone(ranking),
            Note = period.Note,
            Stats = stats,
        };
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
            PeriodKind.LastWeek => RecentObservationPeriodKind.LastWeek,
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

    private static IEnumerable<DataAdjustment?> GetAdjustmentCandidates(DataType dataType, DataAdjustment? preferredAdjustment)
    {
        if (dataType == DataType.Precipitation)
        {
            yield return null;
            yield break;
        }

        if (preferredAdjustment.HasValue)
        {
            yield return preferredAdjustment.Value;
        }

        yield return DataAdjustment.Unadjusted;
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

    private static bool ShouldUseSharedMonthlyHistory(IEnumerable<PeriodObservation> periods)
    {
        return periods.Count(x => x is { CanCompare: true, ComparisonMode: PeriodComparisonMode.MonthlySameMonth }) > 1;
    }

    private static int? GetStartYear(IReadOnlyCollection<DataRecord> records)
    {
        return records.Count == 0 ? null : records.Min(x => (int)x.Year);
    }

    private static int? GetEndYear(IReadOnlyCollection<DataRecord> records)
    {
        return records.Count == 0 ? null : records.Max(x => (int)x.Year);
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
            PeriodKind.LastWeek => "Last week",
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

        if (period.ComparisonMode == PeriodComparisonMode.MonthlySameMonth)
        {
            return MonthName(period.StartDate.Month);
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
            PeriodKind.LastWeek => $"7 days ending {FormatShortDayMonth(endDate)}",
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
            PeriodKind.LastWeek => $"7-day periods ending {FormatShortDayMonth(endDate)}",
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? $"{MonthName(endDate.Month)}s"
                : $"{MonthName(endDate.Month)}-to-date periods",
            PeriodKind.PreviousMonth => $"{MonthName(endDate.Month)}s",
            PeriodKind.YearToDate => "year-to-date periods",
            _ => "comparable periods",
        };
    }

    private static string? CreatePeriodNote(PeriodKind kind, DateOnly endDate, int actualDays, int expectedDays, bool canCompare)
    {
        if (!canCompare)
        {
            return $"Only {actualDays} of {expectedDays} days are available, so the historical comparison is not shown.";
        }

        return null;
    }

    private static string FormatDayMonth(DateOnly date)
    {
        return $"{date.Day} {MonthName(date.Month)}";
    }

    private static string FormatShortDayMonth(DateOnly date)
    {
        return date.ToString("d MMM", CultureInfo.CurrentCulture);
    }

    private static string MonthName(int month)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
    }

    private static string FormatTemperature(double value)
    {
        return $"{value.ToString("0.0", CultureInfo.InvariantCulture)}°C";
    }

    private static string FormatTemperatureAnomaly(double value)
    {
        return $"{(value >= 0 ? "+" : string.Empty)}{FormatTemperature(value)}";
    }

    private static string FormatRainfall(double value)
    {
        return $"{value.ToString("0.#", CultureInfo.InvariantCulture)}mm";
    }

    private static string FormatRainfallAnomaly(double value)
    {
        return $"{(value >= 0 ? "+" : string.Empty)}{FormatRainfall(value)}";
    }

    private static string? FormatHistoricalOccurrence(HistoricalPeriodValue? value)
    {
        return value?.Year?.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record DailyTemperature(DateOnly Date, double Mean, double Max, double Min);

    private sealed record DailyPrecipitation(DateOnly Date, double Rainfall);

    private sealed record PeriodObservation(
        string Title,
        string ComparisonLabel,
        string ComparisonLabelPlural,
        DateOnly StartDate,
        DateOnly EndDate,
        double PrimaryValue,
        double? SupportingValueOne,
        double? SupportingValueTwo,
        int ActualDays,
        int ExpectedDays,
        bool CanCompare,
        PeriodKind Kind,
        PeriodComparisonMode ComparisonMode,
        int? PeriodOffset,
        string? Note);

    private sealed record PreviousMonthPeriod(DateOnly StartDate, DateOnly EndDate, int Offset);

    private sealed record HistoricalValues(List<HistoricalPeriodValue> PeriodValues, int? StartYear, string? UnavailableReason)
    {
        public List<double?> Values => [.. PeriodValues.Select(x => x.Value)];

        public HistoricalPeriodValue? MaxValue => PeriodValues
            .Where(x => x.Value.HasValue)
            .OrderByDescending(x => x.Value!.Value)
            .ThenBy(x => x.Year)
            .FirstOrDefault();

        public HistoricalPeriodValue? MinValue => PeriodValues
            .Where(x => x.Value.HasValue)
            .OrderBy(x => x.Value!.Value)
            .ThenBy(x => x.Year)
            .FirstOrDefault();

        public static HistoricalValues Unavailable(string reason)
        {
            return new HistoricalValues([], null, reason);
        }
    }

    private sealed record HistoricalPeriodValue(double? Value, short? Year);

    private enum PeriodKind
    {
        Daily,
        LastWeek,
        CurrentMonth,
        PreviousMonth,
        CurrentSeason,
        PreviousSeason,
        YearToDate,
    }

    private enum PeriodComparisonMode
    {
        DailyDate,
        MonthlySameMonth,
        DailyRange,
    }
}
#pragma warning restore SA1201, SA1204
