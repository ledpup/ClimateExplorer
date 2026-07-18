namespace ClimateExplorer.DataPipeline;

public sealed class DataPackageRegistry
{
    private readonly HashSet<string> physicalAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> sourcePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> logicalFiles = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> PhysicalAssets => physicalAssets;

    public IReadOnlyCollection<string> SourcePaths => sourcePaths;

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
        if (!sourcePaths.Add(fullSourcePath))
        {
            throw new InvalidDataException($"Source file '{fullSourcePath}' is packaged more than once.");
        }

        RegisterLogicalPath(logicalPath);
    }

    public void RegisterArchiveSource(string sourcePath, IEnumerable<string> logicalPaths)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!sourcePaths.Add(fullSourcePath))
        {
            throw new InvalidDataException($"Source file '{fullSourcePath}' is packaged more than once.");
        }

        foreach (var logicalPath in logicalPaths)
        {
            RegisterLogicalPath(logicalPath);
        }
    }

    private void RegisterLogicalPath(string logicalPath)
    {
        if (!logicalFiles.Add(logicalPath))
        {
            throw new InvalidDataException($"Logical file '{logicalPath}' is emitted more than once.");
        }
    }
}
