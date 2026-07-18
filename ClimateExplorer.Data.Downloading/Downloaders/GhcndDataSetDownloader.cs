namespace ClimateExplorer.Data.Downloading.Downloaders;

using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Workspace;
using ClimateExplorer.Data.Ghcnd;
using static ClimateExplorer.Core.Enums;

public sealed class GhcndDataSetDownloader(HttpClient httpClient) : IDataSetDownloader
{
    private readonly HttpClient httpClient = httpClient;

    public string Key => "ghcnd-station";

    public async Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        var stationIds = request.Measurements.Select(x => x.FileFilter.Id).Distinct(StringComparer.Ordinal).ToList();
        if (stationIds.Count != 1)
        {
            throw new InvalidOperationException("A GHCNd source asset must resolve to exactly one station.");
        }

        var stationId = stationIds[0];
        var csvContent = await GhcndStationCsvDownloader.DownloadCsvAsync(httpClient, stationId, cancellationToken);
        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        await GhcndStationArchiveBuilder.BuildAsync(
            csvContent,
            stationId,
            candidatePath,
            request.Measurements.Any(x => x.MeasurementDefinition.DataType is DataType.TempMax or DataType.TempMin),
            request.Measurements.Any(x => x.MeasurementDefinition.DataType == DataType.Precipitation),
            cancellationToken);
        return new DataSetDownloadArtifact(candidatePath);
    }
}
