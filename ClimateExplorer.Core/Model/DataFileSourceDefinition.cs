namespace ClimateExplorer.Core.Model;

/// <summary>
/// Describes the physical source asset and, when the asset is an archive, the
/// measurement-specific entry within it. Paths are relative to the deployed
/// datasets folder and may contain a <c>[station]</c> placeholder.
/// </summary>
public sealed class DataFileSourceDefinition
{
    public required string FilePathFormat { get; init; }

    public string? ArchiveEntryPathFormat { get; init; }
}
