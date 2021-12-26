public struct YearlyAverageTemps : ITemperatureRecord
{
    public short Year { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
}