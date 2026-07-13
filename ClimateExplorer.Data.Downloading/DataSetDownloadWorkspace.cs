namespace ClimateExplorer.Data.Downloading;

public sealed class DataSetDownloadWorkspace(string path) : IDisposable
{
    public string Path { get; } = path;

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}
