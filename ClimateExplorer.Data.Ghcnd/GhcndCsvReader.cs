namespace ClimateExplorer.Data.Ghcnd;

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

public static class GhcndCsvReader
{
    public static List<GhcndInputRow> ReadRows(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        return ReadRows(reader);
    }

    public static List<GhcndInputRow> ReadRows(TextReader reader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
        };

        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<GhcndInputRow>().ToList();

        records.ForEach(x =>
        {
            x.Prcp = x.Prcp?.Trim('\"').Trim();
            x.Tmax = x.Tmax?.Trim('\"').Trim();
            x.Tmin = x.Tmin?.Trim('\"').Trim();
        });

        return records;
    }

    public static List<GhcndInputRow> RemoveRowsWithNoData(List<GhcndInputRow> rows)
    {
        return rows
            .Where(x => !((string.IsNullOrWhiteSpace(x.Prcp) || x.Prcp == GhcndConstants.NullRecordString)
                        && (string.IsNullOrWhiteSpace(x.Tmax) || x.Tmax == GhcndConstants.NullRecordString)
                        && (string.IsNullOrWhiteSpace(x.Tmin) || x.Tmin == GhcndConstants.NullRecordString)))
            .ToList();
    }
}
