namespace ClimateExplorer.Core.Model;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes a <see cref="DataRecord"/> as a compact 2-element JSON array <c>[dateInt, value]</c>
/// instead of a 4-property object, e.g. <c>[20150824,14.8]</c> rather than
/// <c>{"day":24,"month":8,"year":2015,"value":14.8}</c>. <c>dateInt</c> encodes Year/Month/Day at
/// whichever granularity the record actually has, distinguished by digit count: <c>YYYY</c> (year
/// only), <c>YYYYMM</c> (year+month), <c>YYYYMMDD</c> (year+month+day) - the same three granularities
/// <see cref="DataRecord"/> already supports via its nullable Month/Day. See
/// docs/notes/2026-09-08-01-datarecord-json-payload-size.md for the measurements behind this shape.
/// </summary>
public sealed class DataRecordJsonConverter : JsonConverter<DataRecord>
{
    private const int YearMonthThreshold = 10_000;
    private const int YearMonthDayThreshold = 1_000_000;

    public override DataRecord? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected start of array while reading {nameof(DataRecord)}.");
        }

        reader.Read();
        var dateInt = reader.GetInt32();

        reader.Read();
        double? value = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException($"Expected end of array while reading {nameof(DataRecord)}.");
        }

        var (year, month, day) = Decode(dateInt);

        return new DataRecord(year, month, day, value);
    }

    public override void Write(Utf8JsonWriter writer, DataRecord value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(Encode(value.Year, value.Month, value.Day));

        if (value.Value.HasValue)
        {
            writer.WriteNumberValue(value.Value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WriteEndArray();
    }

    private static int Encode(short year, short? month, short? day)
    {
        if (month.HasValue && day.HasValue)
        {
            return (year * 10000) + (month.Value * 100) + day.Value;
        }

        if (month.HasValue)
        {
            return (year * 100) + month.Value;
        }

        return year;
    }

    private static (short Year, short? Month, short? Day) Decode(int dateInt)
    {
        if (dateInt < YearMonthThreshold)
        {
            return ((short)dateInt, null, null);
        }

        if (dateInt < YearMonthDayThreshold)
        {
            return ((short)(dateInt / 100), (short)(dateInt % 100), null);
        }

        return ((short)(dateInt / 10000), (short)((dateInt / 100) % 100), (short)(dateInt % 100));
    }
}
