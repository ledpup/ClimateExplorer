namespace ClimateExplorer.Core.Model;

public sealed record DataSetStationMetadata
{
    public string? StationId { get; set; }
    public string? StationName { get; set; }
    public DateOnly? StationStartDate { get; set; }
    public DateOnly? StationEndDate { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceUrlLabel { get; set; }
}
