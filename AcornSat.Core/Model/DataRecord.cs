using System.Text.Json.Serialization;

public class DataRecord
{
    public DataRecord()
    {

    }

    public DataRecord(short year, float? value = null)
    {
        Year = year;
        Value = value;
        Key = year.ToString();
    }

    public DataRecord(short year, short? month, short? day, float? value)
    {
        Year = year;
        Month = month;
        Day = day;

        Value = value;

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

    public DataRecord(DateTime date, float? value)
    {
        Year = (short)date.Year;
        Month = (short)date.Month;
        Day = (short)date.Day;

        Value = value;

        Key = $"{Year}_{Month}_{Day}";
    }

    public string Key { get; set; }
    public short? Day { get; set; }
    public short? Month { get; set; }
    public short Year { get; set; }
    public short? Week { get; set; }
    public float? Value { get; set; }

    public string Label { get; set; }

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
}