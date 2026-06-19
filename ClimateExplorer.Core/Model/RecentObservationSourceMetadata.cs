namespace ClimateExplorer.Core.Model;

public sealed record RecentObservationSourceMetadata
{
    public string? SourceCode { get; set; }
    public string? SourceName { get; set; }
    public string? StationId { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceUrlLabel { get; set; }
    public DateTimeOffset? RetrievedAtUtc { get; set; }
}
