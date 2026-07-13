namespace ClimateExplorer.Data.Downloading;

using ClimateExplorer.Core.Model;

public sealed record DataSetDownloadRequest(
    DataSetDefinition DataSetDefinition,
    string DownloaderKey,
    string AssetKey,
    string RelativePath,
    string? DownloadUrl,
    IReadOnlyList<DataSetDownloadMeasurement> Measurements);
