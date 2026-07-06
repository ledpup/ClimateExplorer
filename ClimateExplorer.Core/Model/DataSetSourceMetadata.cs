namespace ClimateExplorer.Core.Model;

public sealed record DataSetSourceMetadata
{
    public Guid? DataSetDefinitionId { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? SourceCode { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceUrlLabel { get; set; }
    public List<DataSetStationMetadata> Stations { get; set; } = [];
}
