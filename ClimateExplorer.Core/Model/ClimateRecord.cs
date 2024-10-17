namespace ClimateExplorer.Core.Model;

using static ClimateExplorer.Core.Enums;

public class ClimateRecord
{
    public DataType DataType { get; set; }
    public RecordType RecordType { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public DataResolution DataResolution { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int? Day { get; set; }
    public double Value { get; set; }
    public int NumberOfTimes { get; set; }
}

public enum RecordType
{
    High,
    Low,
}