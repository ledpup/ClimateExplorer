namespace ClimateExplorer.Data.Downloading;

public sealed class TransformingDataSetDownloader(
    string key,
    DataSetHttpFileDownloader fileDownloader,
    IDataSetSourceFileTransformer transformer) : IDataSetDownloader
{
    private readonly DataSetHttpFileDownloader fileDownloader = fileDownloader;
    private readonly IDataSetSourceFileTransformer transformer = transformer;

    public string Key { get; } = key;

    public async Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        var rawFilePath = Path.Combine(temporaryDirectory, "raw", "download");
        var downloadUrl = request.DownloadUrl
            ?? throw new InvalidOperationException("A transforming HTTP dataset must have a download URL.");
        await fileDownloader.DownloadAsync(downloadUrl, rawFilePath, cancellationToken);

        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(candidatePath)!);
        await transformer.TransformAsync(rawFilePath, candidatePath, cancellationToken);
        if (!File.Exists(candidatePath) || new FileInfo(candidatePath).Length == 0)
        {
            throw new InvalidDataException("Dataset transformation did not produce a source file.");
        }

        return new DataSetDownloadArtifact(candidatePath);
    }
}
