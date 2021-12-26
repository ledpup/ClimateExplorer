using System.Text.Json.Serialization;

public struct DailyTemperatureRecord : ITemperatureRecord
{
    public short Day { get; set; }
    public short Month { get; set; }
    public short Year { get; set; }
    public float? Min { get; set; }
    public float? Max { get; set; }

    [JsonIgnore]
    public DateOnly Date { get { return new DateOnly(Year, Month, Day); } }
}