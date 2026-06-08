namespace ClimateExplorer.Core.Model;

public sealed record LatestRecordsResponse
{
    public List<ClimateRecord> Records { get; set; } = [];
    public bool IsSupported { get; set; }
    public DateTimeOffset? RetrievedDate { get; set; }
}
