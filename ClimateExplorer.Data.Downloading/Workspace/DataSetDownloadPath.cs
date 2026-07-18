namespace ClimateExplorer.Data.Downloading.Workspace;

internal static class DataSetDownloadPath
{
    public static string Resolve(string temporaryDirectory, string relativePath)
    {
        var root = Path.GetFullPath(temporaryDirectory);
        var candidatePath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!candidatePath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Dataset source path resolves outside the temporary directory.");
        }

        return candidatePath;
    }
}
