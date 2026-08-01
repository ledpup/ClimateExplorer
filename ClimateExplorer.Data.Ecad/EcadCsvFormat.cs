namespace ClimateExplorer.Data.Ecad;

using System.Globalization;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The <c>yyyyMMdd,TempMean,TempMax,TempMin,Precipitation</c> shape ClimateExplorer publishes ECA&amp;D in.
/// Both sides of the pipeline use it: the offline build tool writes a station's whole history, and the
/// runtime downloader reads what was last published so it only has to fetch what came after.
/// <para>
/// Rows are date-ordered and carry no header, and a day with no valid observation at all is omitted
/// rather than written as an empty row - the reader fills date gaps with nulls anyway.
/// </para>
/// </summary>
public static class EcadCsvFormat
{
    private const string DateFormat = "yyyyMMdd";

    /// <summary>Two decimal places is more than ECA&amp;D itself publishes, and keeps float noise out of the file.</summary>
    private const string ValueFormat = "0.##";

    public static string Write(IEnumerable<EcadDailyObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        var lines = observations
            .Where(x => x.HasAnyValue)
            .OrderBy(x => x.Date)
            .Select(x => string.Join(
                ',',
                new[] { x.Date.ToString(DateFormat, CultureInfo.InvariantCulture) }
                    .Concat(EcadConstants.PublishedDataTypes.Select(dataType => Format(x[dataType])))));

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    public static SortedDictionary<DateOnly, EcadDailyObservation> Read(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var observations = new SortedDictionary<DateOnly, EcadDailyObservation>();
        foreach (var line in lines)
        {
            var fields = line.Split(',');
            if (fields.Length != EcadConstants.PublishedDataTypes.Count + 1 ||
                !DateOnly.TryParseExact(fields[0], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                continue;
            }

            var observation = new EcadDailyObservation(date);
            for (var i = 0; i < EcadConstants.PublishedDataTypes.Count; i++)
            {
                observation[EcadConstants.PublishedDataTypes[i]] = Parse(fields[i + 1]);
            }

            if (observation.HasAnyValue)
            {
                observations[date] = observation;
            }
        }

        return observations;
    }

    public static DateOnly? GetLatestDate(IEnumerable<string> lines)
    {
        var observations = Read(lines);
        return observations.Count == 0 ? null : observations.Keys.Max();
    }

    private static string Format(double? value)
    {
        return value?.ToString(ValueFormat, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static double? Parse(string field)
    {
        return double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    /// <summary>
    /// The regular expression a measurement definition uses to pull one column out of this file. Group
    /// order follows <see cref="EcadConstants.PublishedDataTypes"/>, so the definitions and the writer
    /// cannot drift apart.
    /// </summary>
    public static string GetDataRowRegEx(DataType dataType)
    {
        var columnIndex = EcadConstants.PublishedDataTypes.ToList().IndexOf(dataType);
        if (columnIndex < 0)
        {
            throw new NotSupportedException($"ECA&D does not publish {dataType}.");
        }

        var columns = EcadConstants.PublishedDataTypes
            .Select((_, i) => i == columnIndex ? @"(?<value>-?\d*\.?\d*)" : @"-?\d*\.?\d*");

        return @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})," + string.Join(',', columns) + "$";
    }
}
