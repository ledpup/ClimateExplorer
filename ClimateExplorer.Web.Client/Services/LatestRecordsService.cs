#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.WebApiClient.Services;
using static ClimateExplorer.Core.Enums;

public sealed class LatestRecordsService : ILatestRecordsService
{
    private const int MinimumHistoricalPeriods = 20;
    private const double MinimumHistoricalCoverage = 0.9d;

    private readonly IDataService dataService;

    public LatestRecordsService(IDataService dataService)
    {
        this.dataService = dataService;
    }

    public async Task<LatestRecordsTabResult> GetTemperatureRecords(Guid locationId)
    {
        var maxTask = dataService.GetLatestRecords(locationId, DataType.TempMax);
        var minTask = dataService.GetLatestRecords(locationId, DataType.TempMin);

        await Task.WhenAll(maxTask, minTask);

        var maxResponse = await maxTask;
        var minResponse = await minTask;

        if (!maxResponse.IsSupported || !minResponse.IsSupported)
        {
            return new LatestRecordsTabResult
            {
                IsSupported = false,
                EmptyMessage = "Latest temperature records are not available for this location.",
            };
        }

        var latestDaily = BuildDailyTemperature(maxResponse.Records, minResponse.Records);
        if (latestDaily.Count == 0)
        {
            return new LatestRecordsTabResult
            {
                EmptyMessage = "No recent temperature records are available yet.",
            };
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var periods = BuildTemperaturePeriods(latestDaily, today);
        if (periods.Count == 0)
        {
            return new LatestRecordsTabResult
            {
                EmptyMessage = "No recent temperature periods can be calculated yet.",
            };
        }

        var preferredAdjustment = maxResponse.DataAdjustment ?? minResponse.DataAdjustment ?? DataAdjustment.Unadjusted;
        var dailyHistory = await GetTemperatureHistoricalDailyRecords(locationId, preferredAdjustment);
        var tiles = new List<LatestRecordTileViewModel>();

        foreach (var period in periods)
        {
            var historicalValues = period.ComparisonMode == PeriodComparisonMode.MonthlySameMonth
                ? await GetTemperatureHistoricalMonthlyValues(locationId, preferredAdjustment, dailyHistory, period)
                : GetTemperatureHistoricalDailyValues(dailyHistory, period);

            tiles.Add(BuildTemperatureTile(period, historicalValues));
        }

        return new LatestRecordsTabResult
        {
            EmptyMessage = "No latest temperature records are available.",
            Tiles = tiles,
        };
    }

    public async Task<LatestRecordsTabResult> GetPrecipitationRecords(Guid locationId)
    {
        var latestResponse = await dataService.GetLatestRecords(locationId, DataType.Precipitation);

        if (!latestResponse.IsSupported)
        {
            return new LatestRecordsTabResult
            {
                IsSupported = false,
                EmptyMessage = "Latest precipitation records are not available for this location.",
            };
        }

        var latestDaily = BuildDailyPrecipitation(latestResponse.Records);
        if (latestDaily.Count == 0)
        {
            return new LatestRecordsTabResult
            {
                EmptyMessage = "No recent precipitation records are available yet.",
            };
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var periods = BuildPrecipitationPeriods(latestDaily, today);
        if (periods.Count == 0)
        {
            return new LatestRecordsTabResult
            {
                EmptyMessage = "No recent precipitation periods can be calculated yet.",
            };
        }

        var dailyHistory = await GetHistoricalRecords(locationId, DataType.Precipitation, null, monthly: false);
        var tiles = new List<LatestRecordTileViewModel>();

        foreach (var period in periods)
        {
            var historicalValues = period.ComparisonMode == PeriodComparisonMode.MonthlySameMonth
                ? await GetPrecipitationHistoricalMonthlyValues(locationId, dailyHistory, period)
                : GetPrecipitationHistoricalDailyValues(dailyHistory, period);

            tiles.Add(BuildPrecipitationTile(period, historicalValues));
        }

        return new LatestRecordsTabResult
        {
            EmptyMessage = "No latest precipitation records are available.",
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

    private static List<PeriodObservation> BuildTemperaturePeriods(List<DailyTemperature> daily, DateOnly today)
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
        if (latestDate.Year == today.Year && latestDate.Month == today.Month)
        {
            var monthRecords = daily
                .Where(x => x.Date.Year == latestDate.Year && x.Date.Month == latestDate.Month && x.Date <= latestDate)
                .ToList();
            AddTemperatureRangePeriod(periods, monthRecords, new DateOnly(latestDate.Year, latestDate.Month, 1), latestDate, PeriodKind.CurrentMonth);
        }

        var lastMonthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        if (lastMonthStart.Year == today.Year)
        {
            var lastMonthEnd = new DateOnly(lastMonthStart.Year, lastMonthStart.Month, DateTime.DaysInMonth(lastMonthStart.Year, lastMonthStart.Month));
            var lastMonthRecords = daily
                .Where(x => x.Date >= lastMonthStart && x.Date <= lastMonthEnd)
                .ToList();
            AddTemperatureRangePeriod(periods, lastMonthRecords, lastMonthStart, lastMonthEnd, PeriodKind.LastMonth);
        }

        if (latestDate.Year == today.Year)
        {
            var ytdRecords = daily
                .Where(x => x.Date.Year == latestDate.Year && x.Date <= latestDate)
                .ToList();
            AddTemperatureRangePeriod(periods, ytdRecords, new DateOnly(latestDate.Year, 1, 1), latestDate, PeriodKind.YearToDate);
        }

        return periods;
    }

    private static List<PeriodObservation> BuildPrecipitationPeriods(List<DailyPrecipitation> daily, DateOnly today)
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
        if (latestDate.Year == today.Year && latestDate.Month == today.Month)
        {
            var monthRecords = daily
                .Where(x => x.Date.Year == latestDate.Year && x.Date.Month == latestDate.Month && x.Date <= latestDate)
                .ToList();
            AddPrecipitationRangePeriod(periods, monthRecords, new DateOnly(latestDate.Year, latestDate.Month, 1), latestDate, PeriodKind.CurrentMonth);
        }

        var lastMonthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        if (lastMonthStart.Year == today.Year)
        {
            var lastMonthEnd = new DateOnly(lastMonthStart.Year, lastMonthStart.Month, DateTime.DaysInMonth(lastMonthStart.Year, lastMonthStart.Month));
            var lastMonthRecords = daily
                .Where(x => x.Date >= lastMonthStart && x.Date <= lastMonthEnd)
                .ToList();
            AddPrecipitationRangePeriod(periods, lastMonthRecords, lastMonthStart, lastMonthEnd, PeriodKind.LastMonth);
        }

        if (latestDate.Year == today.Year)
        {
            var ytdRecords = daily
                .Where(x => x.Date.Year == latestDate.Year && x.Date <= latestDate)
                .ToList();
            AddPrecipitationRangePeriod(periods, ytdRecords, new DateOnly(latestDate.Year, 1, 1), latestDate, PeriodKind.YearToDate);
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
            PeriodComparisonMode.DailyDate,
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
            PeriodComparisonMode.DailyDate,
            null);
    }

    private static void AddTemperatureRangePeriod(List<PeriodObservation> periods, List<DailyTemperature> records, DateOnly startDate, DateOnly endDate, PeriodKind kind)
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
            CreatePeriodTitle(kind, startDate, endDate),
            CreateComparisonLabel(kind, endDate),
            CreateComparisonLabelPlural(kind, endDate),
            startDate,
            endDate,
            records.Average(x => x.Mean),
            records.Average(x => x.Max),
            records.Average(x => x.Min),
            actualDays,
            expectedDays,
            canCompare,
            kind == PeriodKind.LastMonth ? PeriodComparisonMode.MonthlySameMonth : PeriodComparisonMode.DailyRange,
            note));
    }

    private static void AddPrecipitationRangePeriod(List<PeriodObservation> periods, List<DailyPrecipitation> records, DateOnly startDate, DateOnly endDate, PeriodKind kind)
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
            CreatePeriodTitle(kind, startDate, endDate),
            CreateComparisonLabel(kind, endDate),
            CreateComparisonLabelPlural(kind, endDate),
            startDate,
            endDate,
            records.Sum(x => x.Rainfall),
            null,
            null,
            actualDays,
            expectedDays,
            canCompare,
            kind == PeriodKind.LastMonth ? PeriodComparisonMode.MonthlySameMonth : PeriodComparisonMode.DailyRange,
            note));
    }

    private async Task<HistoricalValues> GetTemperatureHistoricalMonthlyValues(
        Guid locationId,
        DataAdjustment? preferredAdjustment,
        ClimateRecordsResponse dailyHistory,
        PeriodObservation period)
    {
        if (!period.CanCompare)
        {
            return HistoricalValues.Unavailable("Latest daily records are incomplete for this period.");
        }

        var monthlyHistory = await GetTemperatureHistoricalMonthlyRecords(locationId, preferredAdjustment, period.StartDate.Month);
        var monthlyValues = monthlyHistory.Records
            .Where(x => x.Value.HasValue && x.Month == period.StartDate.Month && x.Year != period.StartDate.Year)
            .Select(x => x.Value)
            .ToList();

        if (monthlyValues.Count >= MinimumHistoricalPeriods)
        {
            return new HistoricalValues(monthlyValues, monthlyHistory.StartYear, null);
        }

        return GetTemperatureHistoricalDailyValues(dailyHistory, period);
    }

    private async Task<HistoricalValues> GetPrecipitationHistoricalMonthlyValues(Guid locationId, ClimateRecordsResponse dailyHistory, PeriodObservation period)
    {
        if (!period.CanCompare)
        {
            return HistoricalValues.Unavailable("Latest daily records are incomplete for this period.");
        }

        var monthlyHistory = await GetHistoricalRecords(locationId, DataType.Precipitation, null, monthly: true, month: period.StartDate.Month);
        var monthlyValues = monthlyHistory.Records
            .Where(x => x.Value.HasValue && x.Month == period.StartDate.Month && x.Year != period.StartDate.Year)
            .Select(x => x.Value)
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
            return HistoricalValues.Unavailable("Latest daily records are incomplete for this period.");
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
            return HistoricalValues.Unavailable("Latest daily records are incomplete for this period.");
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
            .Select(x => x.Value)
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
                        x.Year != period.StartDate.Year &&
                        IsWithinEquivalentRange(x.Date.Value, period.StartDate, period.EndDate))
            .GroupBy(x => x.Year)
            .Select(group =>
            {
                var expectedDays = GetEquivalentDayCount(group.Key, period.StartDate, period.EndDate);
                var values = group
                    .Select(x => x.Value!.Value)
                    .ToList();
                var requiredDays = (int)Math.Ceiling(expectedDays * MinimumHistoricalCoverage);

                return values.Count >= requiredDays
                    ? (Value: (double?)aggregate(values), Count: values.Count)
                    : (Value: null, Count: values.Count);
            })
            .Where(x => x.Value.HasValue)
            .Select(x => x.Value)
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

    private async Task<ClimateRecordsResponse> GetTemperatureHistoricalMonthlyRecords(Guid locationId, DataAdjustment? preferredAdjustment, int month)
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

    private static LatestRecordTileViewModel BuildTemperatureTile(PeriodObservation period, HistoricalValues historicalValues)
    {
        var ranking = LatestRecordComparison.Rank(period.PrimaryValue, historicalValues.Values);
        var stats = new List<LatestRecordStatViewModel>
        {
            new() { Label = period.ActualDays == 1 ? "Max temp" : "Average max temp", Value = FormatTemperature(period.SupportingValueOne!.Value) },
            new() { Label = period.ActualDays == 1 ? "Min temp" : "Average min temp", Value = FormatTemperature(period.SupportingValueTwo!.Value) },
        };

        if (ranking is not null)
        {
            stats.Add(new LatestRecordStatViewModel { Label = "Historical average", Value = FormatTemperature(ranking.HistoricalAverage) });
            stats.Add(new LatestRecordStatViewModel { Label = "Anomaly", Value = FormatTemperatureAnomaly(ranking.Anomaly) });
        }

        return new LatestRecordTileViewModel
        {
            PeriodTitle = period.Title,
            Headline = ranking is null ? "Comparison unavailable" : LatestRecordComparison.BuildTemperatureHeadline(period.ComparisonLabel, ranking),
            PercentileSentence = ranking is null
                ? historicalValues.UnavailableReason ?? "Not enough historical data is available for this comparison."
                : LatestRecordComparison.BuildTemperaturePercentileSentence(period.ComparisonLabelPlural, historicalValues.StartYear, ranking),
            PrimaryLabel = "Mean temperature",
            PrimaryValue = FormatTemperature(period.PrimaryValue),
            HasComparison = ranking is not null,
            Tone = GetTemperatureTone(ranking),
            Note = period.Note,
            Stats = stats,
        };
    }

    private static LatestRecordTileViewModel BuildPrecipitationTile(PeriodObservation period, HistoricalValues historicalValues)
    {
        var ranking = LatestRecordComparison.Rank(period.PrimaryValue, historicalValues.Values);
        var stats = new List<LatestRecordStatViewModel>();

        if (ranking is not null)
        {
            stats.Add(new LatestRecordStatViewModel { Label = "Historical average", Value = FormatRainfall(ranking.HistoricalAverage) });
            stats.Add(new LatestRecordStatViewModel { Label = "Anomaly", Value = FormatRainfallAnomaly(ranking.Anomaly) });
        }

        if (period.ActualDays > 1)
        {
            stats.Add(new LatestRecordStatViewModel { Label = "Days available", Value = $"{period.ActualDays}/{period.ExpectedDays}" });
        }

        return new LatestRecordTileViewModel
        {
            PeriodTitle = period.Title,
            Headline = ranking is null ? "Comparison unavailable" : LatestRecordComparison.BuildPrecipitationHeadline(period.ComparisonLabel, ranking),
            PercentileSentence = ranking is null
                ? historicalValues.UnavailableReason ?? "Not enough historical data is available for this comparison."
                : LatestRecordComparison.BuildPrecipitationPercentileSentence(historicalValues.StartYear, ranking),
            PrimaryLabel = "Rainfall total",
            PrimaryValue = FormatRainfall(period.PrimaryValue),
            HasComparison = ranking is not null,
            Tone = GetPrecipitationTone(ranking),
            Note = period.Note,
            Stats = stats,
        };
    }

    private static LatestRecordTileTone GetTemperatureTone(LatestRecordComparisonResult? ranking)
    {
        return ranking?.Direction switch
        {
            LatestRecordComparisonDirection.High => LatestRecordTileTone.TemperatureWarm,
            LatestRecordComparisonDirection.Low => LatestRecordTileTone.TemperatureCool,
            null => LatestRecordTileTone.Unavailable,
            _ => LatestRecordTileTone.Neutral,
        };
    }

    private static LatestRecordTileTone GetPrecipitationTone(LatestRecordComparisonResult? ranking)
    {
        return ranking?.Direction switch
        {
            LatestRecordComparisonDirection.High => LatestRecordTileTone.PrecipitationWet,
            LatestRecordComparisonDirection.Low => LatestRecordTileTone.PrecipitationDry,
            null => LatestRecordTileTone.Unavailable,
            _ => LatestRecordTileTone.Neutral,
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

        if (preferredAdjustment != DataAdjustment.Unadjusted)
        {
            yield return DataAdjustment.Unadjusted;
        }

        if (preferredAdjustment != DataAdjustment.Adjusted)
        {
            yield return DataAdjustment.Adjusted;
        }
    }

    private static bool IsWithinEquivalentRange(DateOnly date, DateOnly templateStart, DateOnly templateEnd)
    {
        var dateMonthDay = (date.Month * 100) + date.Day;
        var startMonthDay = (templateStart.Month * 100) + templateStart.Day;
        var endMonthDay = (templateEnd.Month * 100) + templateEnd.Day;

        return dateMonthDay >= startMonthDay && dateMonthDay <= endMonthDay;
    }

    private static int GetEquivalentDayCount(short year, DateOnly templateStart, DateOnly templateEnd)
    {
        var startDate = CreateEquivalentDate(year, templateStart.Month, templateStart.Day);
        var endDate = CreateEquivalentDate(year, templateEnd.Month, templateEnd.Day);
        return endDate.DayNumber - startDate.DayNumber + 1;
    }

    private static DateOnly CreateEquivalentDate(int year, int month, int day)
    {
        return new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
    }

    private static int GetDayCount(DateOnly startDate, DateOnly endDate)
    {
        return endDate.DayNumber - startDate.DayNumber + 1;
    }

    private static int? GetStartYear(IReadOnlyCollection<DataRecord> records)
    {
        return records.Count == 0 ? null : records.Min(x => (int)x.Year);
    }

    private static int? GetEndYear(IReadOnlyCollection<DataRecord> records)
    {
        return records.Count == 0 ? null : records.Max(x => (int)x.Year);
    }

    private static string CreatePeriodTitle(PeriodKind kind, DateOnly startDate, DateOnly endDate)
    {
        return kind switch
        {
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? $"{MonthName(endDate.Month)} {endDate.Year}"
                : $"{MonthName(endDate.Month)} {endDate.Year} to date",
            PeriodKind.LastMonth => $"{MonthName(startDate.Month)} {startDate.Year}",
            PeriodKind.YearToDate => $"{endDate.Year} to date",
            _ => string.Empty,
        };
    }

    private static string CreateComparisonLabel(PeriodKind kind, DateOnly endDate)
    {
        return kind switch
        {
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? MonthName(endDate.Month)
                : $"{MonthName(endDate.Month)} to date",
            PeriodKind.LastMonth => MonthName(endDate.Month),
            PeriodKind.YearToDate => "year to date",
            _ => string.Empty,
        };
    }

    private static string CreateComparisonLabelPlural(PeriodKind kind, DateOnly endDate)
    {
        return kind switch
        {
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? $"{MonthName(endDate.Month)}s"
                : $"{MonthName(endDate.Month)}-to-date periods",
            PeriodKind.LastMonth => $"{MonthName(endDate.Month)}s",
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

        return kind switch
        {
            PeriodKind.CurrentMonth => endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month)
                ? null
                : $"Compared with equivalent historical month-to-date periods through {FormatDayMonth(endDate)}.",
            PeriodKind.YearToDate => $"Compared with equivalent historical year-to-date periods through {FormatDayMonth(endDate)}.",
            _ => null,
        };
    }

    private static string FormatDayMonth(DateOnly date)
    {
        return $"{date.Day} {MonthName(date.Month)}";
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
        PeriodComparisonMode ComparisonMode,
        string? Note);

    private sealed record HistoricalValues(List<double?> Values, int? StartYear, string? UnavailableReason)
    {
        public static HistoricalValues Unavailable(string reason)
        {
            return new HistoricalValues([], null, reason);
        }
    }

    private enum PeriodKind
    {
        CurrentMonth,
        LastMonth,
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
