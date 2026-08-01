namespace ClimateExplorer.Data.Ecad;

using System.Globalization;
using System.Text.Json;
using ClimateExplorer.Core;

/// <summary>
/// Reads the GeoJSON <c>FeatureCollection</c> returned by the collection's <c>/locations</c> endpoint.
/// The listing is not paginated - a single response carries every station - so it is read as one document,
/// but from a stream rather than a string because it runs to roughly ten megabytes.
/// </summary>
public static class EcadStationCatalogueReader
{
    public static async Task<IReadOnlyList<EcadStation>> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("The ECA&D locations listing did not contain a GeoJSON feature array.");
        }

        var stations = new List<EcadStation>(features.GetArrayLength());
        foreach (var feature in features.EnumerateArray())
        {
            var station = ReadStation(feature);
            if (station != null)
            {
                stations.Add(station);
            }
        }

        if (stations.Count == 0)
        {
            throw new InvalidDataException("The ECA&D locations listing contained no usable stations.");
        }

        return stations;
    }

    private static EcadStation? ReadStation(JsonElement feature)
    {
        if (!feature.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        if (!feature.TryGetProperty("geometry", out var geometry) ||
            !geometry.TryGetProperty("coordinates", out var coordinates) ||
            coordinates.ValueKind != JsonValueKind.Array ||
            coordinates.GetArrayLength() < 2)
        {
            return null;
        }

        // GeoJSON orders a position as longitude, latitude.
        var longitude = coordinates[0].GetDouble();
        var latitude = coordinates[1].GetDouble();

        feature.TryGetProperty("properties", out var properties);

        return new EcadStation(
            id.GetString()!,
            GetOptionalString(properties, "station_name"),
            GetOptionalString(properties, "country_code"),
            new Coordinates
            {
                Latitude = latitude,
                Longitude = longitude,
                Elevation = GetOptionalElevation(properties),
            },
            ReadSeries(properties));
    }

    /// <summary>
    /// A station's reported parameter codes and their date ranges are nested under
    /// <c>properties.provider.{contributor name}.{parameter code}</c> as a list of
    /// <c>[first, last]</c> pairs. Reading them here is what lets the ECA&amp;D integration pick the one
    /// accumulation-period variant a station actually reports, instead of probing every variant blindly.
    /// </summary>
    private static IReadOnlyList<EcadStationSeries> ReadSeries(JsonElement properties)
    {
        if (properties.ValueKind != JsonValueKind.Object ||
            !properties.TryGetProperty("provider", out var providers) ||
            providers.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var spans = new Dictionary<string, (DateOnly First, DateOnly Last)>(StringComparer.Ordinal);
        foreach (var provider in providers.EnumerateObject())
        {
            if (provider.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var parameter in provider.Value.EnumerateObject())
            {
                foreach (var interval in EnumerateIntervals(parameter.Value))
                {
                    spans[parameter.Name] = spans.TryGetValue(parameter.Name, out var existing)
                        ? (First: existing.First < interval.First ? existing.First : interval.First,
                           Last: existing.Last > interval.Last ? existing.Last : interval.Last)
                        : interval;
                }
            }
        }

        return [.. spans
            .Select(x => new EcadStationSeries(x.Key, x.Value.First, x.Value.Last))
            .OrderBy(x => x.ParameterCode, StringComparer.Ordinal)];
    }

    private static IEnumerable<(DateOnly First, DateOnly Last)> EnumerateIntervals(JsonElement intervals)
    {
        if (intervals.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var interval in intervals.EnumerateArray())
        {
            if (interval.ValueKind != JsonValueKind.Array || interval.GetArrayLength() < 2)
            {
                continue;
            }

            if (TryReadDate(interval[0], out var first) && TryReadDate(interval[1], out var last) && last >= first)
            {
                yield return (first, last);
            }
        }
    }

    private static bool TryReadDate(JsonElement element, out DateOnly date)
    {
        date = default;
        var text = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        if (text == null || !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return false;
        }

        date = DateOnly.FromDateTime(parsed);
        return true;
    }

    private static string? GetOptionalString(JsonElement properties, string propertyName)
    {
        return properties.ValueKind == JsonValueKind.Object &&
            properties.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double? GetOptionalElevation(JsonElement properties)
    {
        return properties.ValueKind == JsonValueKind.Object &&
            properties.TryGetProperty("height_above_mean_sea_level", out var value) &&
            value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;
    }
}
