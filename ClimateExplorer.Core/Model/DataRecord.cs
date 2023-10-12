using ClimateExplorer.Core.DataPreparation;
using System.Text.Json.Serialization;

namespace ClimateExplorer.Core.Model;
public class DataRecord
{
    public DataRecord()
    {

    }

    public DataRecord(short year, float? value = null)
    {
        Year = year;
        Value = value;
        CreateKey();
    }

    public DataRecord(short year, short? month, short? day, float? value)
    {
        Year = year;
        Month = month;
        Day = day;

        Value = value;

        CreateKey();
    }

    public DataRecord(DateTime date, float? value)
    {
        Year = (short)date.Year;
        Month = (short)date.Month;
        Day = (short)date.Day;

        Value = value;

        CreateKey();
    }

    public string? Key { get; set; }
    public short? Day { get; set; }
    public short? Month { get; set; }
    public short Year { get; set; }
    public short? Week { get; set; }
    public float? Value { get; set; }

    public string? Label { get; set; }
    public string? BinId { get; set; }

    BinIdentifier? _cachedParsedBinId;

    public BinIdentifier? GetBinIdentifier()
    {
        if (_cachedParsedBinId == null)
        {
            _cachedParsedBinId = BinIdentifier.Parse(BinId!);
        }

        return _cachedParsedBinId;
    }

    [JsonIgnore]
    public DateTime? Date
    { 
        get 
        {
            if (Month.HasValue && Day.HasValue)
            {
                return new DateTime(Year, Month.Value, Day.Value);
            }
            return null;
        } 
    }

    private void CreateKey()
    {
        Key = Year.ToString();
        if (Month != null)
        {
            Key += "_" + Month;
        }
        if (Day != null)
        {
            Key += "_" + Day;
        }
    }
    public override string ToString()
    {
        return $"{Year}-{Month}-{Day}: {Value}";
    }
}