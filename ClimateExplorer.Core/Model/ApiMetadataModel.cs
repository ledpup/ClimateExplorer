namespace ClimateExplorer.Core.Model;

public sealed record ApiMetadataModel
{
    public required string Version { get; set; }
    public DateTime BuildTimeUtc { get; set; }
}
