namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.DataPreparation;
using System.Text.Json.Serialization;

public class DataRecord
{
    private BinIdentifier? cachedParsedBinId;

    public DataRecord()
    {
    }

    public DataRecord(short year, double? value = null)
    {
        Year = year;
        Value = value;
        CreateKey();
    }

    public DataRecord(short year, short? month, short? day, double? value)
    {
        Year = year;
        Month = month;
        Day = day;

        Value = value;

        CreateKey();
    }

    public DataRecord(DateTime date, double? value)
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
    public double? Value { get; set; }

    public string? Label { get; set; }
    public string? BinId { get; set; }

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

    public BinIdentifier? GetBinIdentifier()
    {
        if (cachedParsedBinId == null)
        {
            cachedParsedBinId = BinIdentifier.Parse(BinId!);
        }

        return cachedParsedBinId;
    }

    public override string ToString()
    {
        return $"{Year}-{Month}-{Day}: {Value}";
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
}