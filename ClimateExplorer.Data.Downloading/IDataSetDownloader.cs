namespace ClimateExplorer.Data.Downloading;

public interface IDataSetDownloader
{
    string Key { get; }

    Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken);
}
