namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;

// Turns the raw per-DataType records for a domain into the single merged
// DailyObservation series that periods and historical distributions are built from.
public sealed partial class RecentObservationsCalculator
{
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
        return BuildDailySeries(records, (date, value) => new DailyObservation(date, null, null, null, value));
    }

    private static List<DailyObservation> BuildDailyCo2(IEnumerable<DataRecord> records)
    {
        return BuildDailySeries(records, (date, value) => new DailyObservation(date, null, null, null, null, value));
    }

    private static List<DailyObservation> BuildDailyTemperatureMean(IEnumerable<DataRecord> records)
    {
        return BuildDailySeries(records, (date, value) => new DailyObservation(date, null, null, value, null));
    }

    // Shared by the single-value domains (precipitation, CO2, temperature-mean-only
    // history): each just places its one measurement into a different DailyObservation slot.
    private static List<DailyObservation> BuildDailySeries(
        IEnumerable<DataRecord> records,
        Func<DateOnly, double, DailyObservation> createObservation)
    {
        return [.. records
            .Where(x => x.Date.HasValue && x.Value.HasValue)
            .Select(x => createObservation(x.Date!.Value, x.Value!.Value))
            .OrderBy(x => x.Date)];
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

    private static int? GetStartYear(IReadOnlyCollection<DailyObservation> records)
    {
        return records.Count == 0 ? null : records.Min(x => x.Date.Year);
    }
}
