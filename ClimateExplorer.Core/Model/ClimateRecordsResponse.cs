namespace ClimateExplorer.Core.Model;

using static ClimateExplorer.Core.Enums;

public sealed record ClimateRecordsResponse
{
    public List<DataRecord> Records { get; set; } = [];
    public DataType? DataType { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public DataResolution? DataResolution { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
    public int TotalCount { get; set; }
}
