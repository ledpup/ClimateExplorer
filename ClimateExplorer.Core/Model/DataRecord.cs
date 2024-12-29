namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.DataPreparation;
using System.Text.Json.Serialization;

public class DataRecord
{
    public DataRecord(short year, short? month, short? day, double? value)
    {
        Year = year;
        Month = month;
        Day = day;

        Value = value;

        CreateKey();
    }

    public DataRecord(short year, short? month, double? value)
    {
        Year = year;
        Month = month;

        Value = value;

        CreateKey();
    }

    public DataRecord(DateOnly date, double? value)
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
    public double? Value { get; set; }

    [JsonIgnore]
    public DateOnly? Date
    {
        get
        {
            if (Month.HasValue && Day.HasValue)
            {
                return new DateOnly(Year, Month.Value, Day.Value);
            }

            return null;
        }
    }

    public DataRecord WithValue(double? value)
    {
        return new DataRecord(Year, Month, Day, value);
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