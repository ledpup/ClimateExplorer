namespace ClimateExplorer.Core.Model;

using static ClimateExplorer.Core.Enums;

public sealed record RecentObservationsResponse
{
    public List<DataRecord> Records { get; set; } = [];
    public DataType? DataType { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public DataResolution? DataResolution { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public bool IsSupported { get; set; }
    public DateTimeOffset? RetrievedDate { get; set; }
    public RecentObservationSourceMetadata? SourceMetadata { get; set; }
}
