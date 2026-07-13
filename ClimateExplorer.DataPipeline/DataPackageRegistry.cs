namespace ClimateExplorer.DataPipeline;

public sealed class DataPackageRegistry
{
    private readonly HashSet<string> physicalAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> sourceToLogicalFile = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> logicalFiles = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> PhysicalAssets => physicalAssets;

    public IReadOnlyCollection<string> SourcePaths => sourceToLogicalFile.Keys;

    public int LogicalFileCount => logicalFiles.Count;

    public void RegisterPhysicalAsset(string relativePath)
    {
        if (!physicalAssets.Add(relativePath))
        {
            throw new InvalidDataException($"Physical asset '{relativePath}' is emitted more than once.");
        }
    }

    public void RegisterLogicalFile(string sourcePath, string logicalPath)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (sourceToLogicalFile.TryGetValue(fullSourcePath, out var existingLogicalPath))
        {
            throw new InvalidDataException($"Source file '{fullSourcePath}' is packaged twice as '{existingLogicalPath}' and '{logicalPath}'.");
        }

        if (!logicalFiles.Add(logicalPath))
        {
            throw new InvalidDataException($"Logical file '{logicalPath}' is emitted more than once.");
        }

        sourceToLogicalFile.Add(fullSourcePath, logicalPath);
    }
}
