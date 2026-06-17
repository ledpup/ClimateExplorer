namespace ClimateExplorer.Data.Ghcnd;

using Microsoft.Extensions.Logging;

public static class GhcndTemperatureProcessor
{
    public static List<OutputRowTemperature> CreateRecords(IEnumerable<GhcndInputRow> rows)
    {
        return rows.Select(x => new OutputRowTemperature
        {
            Date = x.Date?.Replace("-", string.Empty),
            Tmax = x.TmaxQflag || string.IsNullOrWhiteSpace(x.Tmax) ? GhcndConstants.NullRecord : int.Parse(x.Tmax),
            Tmin = x.TminQflag || string.IsNullOrWhiteSpace(x.Tmin) ? GhcndConstants.NullRecord : int.Parse(x.Tmin),
        }).ToList();
    }

    // Any temperature value above 60°C (600 in the GHCNd dataset as temperatures are tenths of a degree) or below -100°C (-1000 in the GHCNd dataset) is likely an error and will be set to null.
    // The highest temperature ever recorded on Earth is 56.7°C.
    // The lowest natural temperature ever directly recorded at ground level on Earth is −89.2°C.
    public static void ValidateRecords(List<OutputRowTemperature> records, string stationId, ILogger logger)
    {
        records.ForEach(x =>
        {
            if (x.Tmax != GhcndConstants.NullRecord && (x.Tmax > 600 || x.Tmax < -1000))
            {
                logger.LogError($"Valid temperature value ({x.Tmax}) exceeded for {stationId}. Setting Tmax to null.");
                x.Tmax = GhcndConstants.NullRecord;
            }

            if (x.Tmin != GhcndConstants.NullRecord && (x.Tmin > 600 || x.Tmin < -1000))
            {
                logger.LogError($"Valid temperature value ({x.Tmin}) exceeded for {stationId}. Setting Tmin to null.");
                x.Tmin = GhcndConstants.NullRecord;
            }
        });
    }

    public static List<OutputRowTemperature> FilterSufficientData(IEnumerable<OutputRowTemperature> records)
    {
        var yearRecordCount = new Dictionary<string, TempYearCount>();
        foreach (var record in records)
        {
            var year = record.Date!.Substring(0, 4);
            if (!yearRecordCount.ContainsKey(year))
            {
                yearRecordCount.Add(year, new TempYearCount());
            }

            yearRecordCount[year].TMax += record.Tmax == GhcndConstants.NullRecord ? 0 : 1;
            yearRecordCount[year].TMin += record.Tmin == GhcndConstants.NullRecord ? 0 : 1;
        }

        var yearsWithMinimumNumberOfRecords = yearRecordCount
            .Where(x => x.Value.TMax > 300 && x.Value.TMin > 300)
            .Select(x => x.Key)
            .ToHashSet();

        return records.Where(x => yearsWithMinimumNumberOfRecords.Contains(x.Date!.Substring(0, 4))).ToList();
    }

    private sealed class TempYearCount
    {
        public int TMax { get; set; }
        public int TMin { get; set; }
    }
}
