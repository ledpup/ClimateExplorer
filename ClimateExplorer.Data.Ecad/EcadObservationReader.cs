namespace ClimateExplorer.Data.Ecad;

using System.Globalization;
using System.Text.Json;

/// <summary>
/// Reads the CoverageJSON a station data query returns. The payload is a single coverage holding a time
/// axis and one parallel value array per parameter, each paired with a <c>{code}_q</c> quality flag array.
/// A missing day is JSON <c>null</c> in both.
/// </summary>
public static class EcadObservationReader
{
    /// <summary>
    /// Returns, per parameter code, the valid observations in the response. Values whose quality flag is
    /// anything other than <see cref="EcadConstants.ValidQualityFlag"/> are dropped rather than published,
    /// matching how GHCNd's quality flags are treated by <c>GhcndTemperatureProcessor</c>.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<DateOnly, double>>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var coverage = GetSingleCoverage(document.RootElement);
        if (coverage is not { } coverageElement)
        {
            return new Dictionary<string, IReadOnlyDictionary<DateOnly, double>>(StringComparer.Ordinal);
        }

        var dates = ReadDates(coverageElement);
        if (!coverageElement.TryGetProperty("ranges", out var ranges) || ranges.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, IReadOnlyDictionary<DateOnly, double>>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, IReadOnlyDictionary<DateOnly, double>>(StringComparer.Ordinal);
        foreach (var range in ranges.EnumerateObject())
        {
            if (range.Name.EndsWith("_q", StringComparison.Ordinal))
            {
                continue;
            }

            var values = ReadValues(range.Value);
            var qualityFlags = ranges.TryGetProperty(range.Name + "_q", out var flagRange)
                ? ReadValues(flagRange)
                : null;

            var observations = new Dictionary<DateOnly, double>();
            for (var i = 0; i < dates.Count && i < values.Count; i++)
            {
                if (values[i] is not { } value)
                {
                    continue;
                }

                // A quality flag array shorter than the value array, or absent entirely, would mean the
                // response is not the shape documented; treating that as "unflagged" would silently
                // publish suspect values, so it is treated as unusable instead.
                if (qualityFlags == null || i >= qualityFlags.Count ||
                    qualityFlags[i] is not { } flag ||
                    (int)flag != EcadConstants.ValidQualityFlag)
                {
                    continue;
                }

                observations[dates[i]] = value;
            }

            if (observations.Count > 0)
            {
                result.Add(range.Name, observations);
            }
        }

        return result;
    }

    private static JsonElement? GetSingleCoverage(JsonElement root)
    {
        if (!root.TryGetProperty("coverages", out var coverages) || coverages.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var count = coverages.GetArrayLength();
        return count switch
        {
            0 => null,
            1 => coverages[0],
            _ => throw new InvalidDataException($"An ECA&D single-station query returned {count} coverages; expected one."),
        };
    }

    private static IReadOnlyList<DateOnly> ReadDates(JsonElement coverage)
    {
        if (!coverage.TryGetProperty("domain", out var domain) ||
            !domain.TryGetProperty("axes", out var axes) ||
            !axes.TryGetProperty("t", out var timeAxis) ||
            !timeAxis.TryGetProperty("values", out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("An ECA&D coverage did not contain a time axis.");
        }

        var dates = new List<DateOnly>(values.GetArrayLength());
        foreach (var value in values.EnumerateArray())
        {
            var text = value.GetString();
            if (text == null ||
                !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            {
                throw new InvalidDataException($"An ECA&D coverage contained an unreadable timestamp '{text}'.");
            }

            dates.Add(DateOnly.FromDateTime(parsed));
        }

        return dates;
    }

    private static IReadOnlyList<double?> ReadValues(JsonElement range)
    {
        if (!range.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<double?>(values.GetArrayLength());
        foreach (var value in values.EnumerateArray())
        {
            result.Add(value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null);
        }

        return result;
    }
}
