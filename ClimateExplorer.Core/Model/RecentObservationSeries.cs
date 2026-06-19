namespace ClimateExplorer.Core.Model;

using static ClimateExplorer.Core.Enums;

/// <summary>
/// A single recent-observation series (max temperature, min temperature or
/// precipitation) with its measurement metadata and provenance. Provenance is
/// kept per series so the panel's notes section can list each distinct download
/// (e.g. the two BOM obs-code files behind the temperature tab) while collapsing
/// series that share a source (e.g. the single GHCNd station CSV).
/// </summary>
public sealed record RecentObservationSeries
{
    public List<DataRecord> Records { get; set; } = [];
    public DataAdjustment? DataAdjustment { get; set; }
    public DataResolution? DataResolution { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public RecentObservationSourceMetadata? SourceMetadata { get; set; }
}
