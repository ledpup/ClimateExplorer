using System.Text.Json.Serialization;

public class DataRecord
{
    public DataRecord()
    {

    }

    public DataRecord(short year, short month, short? day, float? value)
    {
        Year = year;
        Month = month;
        Day = day;

        Value = value;

        Week = null;
    }

    public DataRecord(DateTime date, float? value)
    {
        Year = (short)date.Year;
        Month = (short)date.Month;
        Day = (short)date.Day;

        Value = value;

        Week = null;
    }
    public short? Day { get; set; }
    public short? Month { get; set; }
    public short Year { get; set; }
    public short? Week { get; set; }
    public float? Value { get; set; }

    public string Label { get; set; }

    [JsonIgnore]
    public DateTime Date { get { return new DateTime(Year, Month.Value, Day.Value); } }
}