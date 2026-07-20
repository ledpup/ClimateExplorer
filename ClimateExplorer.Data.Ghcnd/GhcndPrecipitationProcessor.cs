namespace ClimateExplorer.Data.Ghcnd;

using Microsoft.Extensions.Logging;

public static class GhcndPrecipitationProcessor
{
    public static List<OutputRowPrecipitation> CreateRecords(IEnumerable<GhcndInputRow> rows)
    {
        return rows.Select(x => new OutputRowPrecipitation
        {
            Date = x.Date?.Replace("-", string.Empty),
            Precipitation = x.PrcpQflag || string.IsNullOrWhiteSpace(x.Prcp) ? GhcndConstants.NullRecord : int.Parse(x.Prcp),
        }).ToList();
    }

    // Any precipitation value above 2000 mm (20000 in the GHCNd dataset as precipitation values are tenths of a mm) is likely an error and will be set to null.
    // The highest officially recognized 24-hour rainfall in the world is 1,825 mm
    public static void ValidateRecords(List<OutputRowPrecipitation> records, string stationId, ILogger logger)
    {
        records.ForEach(x =>
        {
            if (x.Precipitation != GhcndConstants.NullRecord && (x.Precipitation > 20000 || x.Precipitation < 0))
            {
                logger.LogError($"Valid precipitation value ({x.Precipitation}) exceeded for {stationId}. Setting Precipitation to null.");
                x.Precipitation = GhcndConstants.NullRecord;
            }
        });
    }

    // Used to decide whether a station's data is worth including at all (e.g. excludes stations with only
    // a handful of scattered readings across their history). This is a station-level inclusion gate, not a
    // data filter - it does not remove any rows, including partial years such as the current, in-progress year.
    public static bool HasSufficientData(IEnumerable<OutputRowPrecipitation> records)
    {
        var yearRecordCount = new Dictionary<string, int>();
        foreach (var record in records)
        {
            var year = record.Date!.Substring(0, 4);
            if (!yearRecordCount.ContainsKey(year))
            {
                yearRecordCount.Add(year, 0);
            }

            yearRecordCount[year] += record.Precipitation == GhcndConstants.NullRecord ? 0 : 1;
        }

        return yearRecordCount.Values.Any(x => x > 300);
    }
}
