namespace ClimateExplorer.Data.Downloading.Downloaders;

using ClimateExplorer.Data.Downloading.Models;

public interface IDataSetDownloader
{
    string Key { get; }

    Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken);
}
