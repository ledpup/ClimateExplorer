namespace ClimateExplorer.Core.DataPreparation;

public struct TemporalDataPoint
{
    public TemporalDataPoint(short year, short? month, short? day, double? value)
    {
        Year = year;
        Month = month;
        Day = day;
        Value = value;
    }

    public TemporalDataPoint(DateOnly d, double? value)
    {
        Year = (short)d.Year;
        Month = (short)d.Month;
        Day = (short)d.Day;
        Value = value;
    }

    public short Year { get; set; }
    public short? Month { get; set; }
    public short? Day { get; set; }
    public double? Value { get; set; }

    public TemporalDataPoint WithValue(double? value)
    {
        return
            new TemporalDataPoint
            {
                Year = Year,
                Month = Month,
                Day = Day,
                Value = value,
            };
    }

    public override string ToString()
    {
        return $"{Year}-{Month}-{Day}:{Value}";
    }
}
