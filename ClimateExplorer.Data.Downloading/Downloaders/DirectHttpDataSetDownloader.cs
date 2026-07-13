namespace ClimateExplorer.Data.Downloading.Downloaders;

using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Workspace;

public sealed class DirectHttpDataSetDownloader(DataSetHttpFileDownloader fileDownloader) : IDataSetDownloader
{
    private readonly DataSetHttpFileDownloader fileDownloader = fileDownloader;

    public string Key => "direct-http";

    public async Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryDirectory);

        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        var downloadUrl = request.DownloadUrl
            ?? throw new InvalidOperationException("A direct HTTP dataset must have a download URL.");
        await fileDownloader.DownloadAsync(downloadUrl, candidatePath, cancellationToken);
        return new DataSetDownloadArtifact(candidatePath);
    }
}
