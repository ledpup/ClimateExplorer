namespace ClimateExplorer.WebApi.Infrastructure;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;

/// <summary>
/// Serializer options for the file-backed caches. Output is compact, and a few high-volume values are rounded on
/// write to the precision that is meaningful (or displayed), to keep the cache files small.
/// </summary>
/// <remarks>
/// Rounding applies only when writing. It must not feed back into calculations: heating scores and their bands are
/// derived from the full-precision warming anomalies before the locations are cached.
/// </remarks>
internal static class CacheJsonOptions
{
    private const int WarmingAnomalyDecimalPlaces = 3;
    private const int CoordinateDecimalPlaces = 6;
    private const int ElevationDecimalPlaces = 0;

    public static JsonSerializerOptions Create() =>
        new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ApplyRounding } },
        };

    private static void ApplyRounding(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(Location))
        {
            RoundProperty(typeInfo, nameof(Location.WarmingAnomaly), WarmingAnomalyDecimalPlaces);
        }
        else if (typeInfo.Type == typeof(Coordinates))
        {
            RoundProperty(typeInfo, nameof(Coordinates.Latitude), CoordinateDecimalPlaces);
            RoundProperty(typeInfo, nameof(Coordinates.Longitude), CoordinateDecimalPlaces);
            RoundProperty(typeInfo, nameof(Coordinates.Elevation), ElevationDecimalPlaces);
        }
    }

    private static void RoundProperty(JsonTypeInfo typeInfo, string propertyName, int decimalPlaces)
    {
        foreach (var property in typeInfo.Properties)
        {
            if (property.Name != propertyName)
            {
                continue;
            }

            if (property.PropertyType == typeof(double))
            {
                property.CustomConverter = new RoundedDoubleConverter(decimalPlaces);
            }
            else if (property.PropertyType == typeof(double?))
            {
                property.CustomConverter = new RoundedNullableDoubleConverter(decimalPlaces);
            }
            else
            {
                throw new InvalidOperationException($"{typeInfo.Type.Name}.{propertyName} is {property.PropertyType.Name}, not a double.");
            }
        }
    }

    // Math.Round turns tiny negatives into -0, which would be written as "-0".
    private static double Round(double value, int decimalPlaces)
    {
        var rounded = Math.Round(value, decimalPlaces);
        return rounded == 0 ? 0 : rounded;
    }

    private sealed class RoundedDoubleConverter(int decimalPlaces) : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(Round(value, decimalPlaces));
    }

    private sealed class RoundedNullableDoubleConverter(int decimalPlaces) : JsonConverter<double?>
    {
        public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(Round(value.Value, decimalPlaces));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
