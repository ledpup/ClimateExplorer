namespace ClimateExplorer.Web.Client.UiModel;

using ClimateExplorer.Core.Model;

public sealed class ClimateRecordViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? Day { get; set; }
    public double Value { get; set; }
    public double? Anomaly { get; set; }
    public double? Average { get; set; }

    public static ClimateRecordViewModel FromDataRecord(DataRecord record)
    {
        return new ClimateRecordViewModel
        {
            Year = record.Year,
            Month = record.Month!.Value,
            Day = record.Day,
            Value = record.Value!.Value,
        };
    }

    public DataRecord ToDataRecord()
    {
        return new DataRecord((short)Year, (short)Month, Day.HasValue ? (short)Day.Value : null, Value);
    }
}
