using System.Text.Json.Serialization;
using static AcornSat.Core.Enums;

public class TemperatureRecord
{
    public TemperatureRecord()
    {

    }
    public TemperatureRecord(DateTime date, float? min, float? max)
    {
        Year = (short)date.Year;
        Month = (short)date.Month;
        Day = (short)date.Day;

        Min = min;
        Max = max;

        Week = null;
    }
    public short? Day { get; set; }
    public short? Month { get; set; }
    public short Year { get; set; }
    public short? Week { get; set; }
    public float? Min { get; set; }
    public float? Max { get; set; }

    [JsonIgnore]
    public DateTime Date { get { return new DateTime(Year, Month.Value, Day.Value); } }
}