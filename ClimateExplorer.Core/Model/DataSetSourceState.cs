namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.Interface;

public sealed class DataSetSourceState : ICachedData
{
    public required string AssetKey { get; set; }

    public required string RelativePath { get; set; }

    public required long Length { get; set; }

    public required string Sha256 { get; set; }

    public DateTimeOffset? RetrievedDate { get; set; }
}
