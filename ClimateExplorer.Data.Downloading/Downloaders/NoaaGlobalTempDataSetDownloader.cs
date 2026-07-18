namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.Globalization;
using System.Net.Http;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Workspace;

public sealed class NoaaGlobalTempDataSetDownloader(DataSetHttpFileDownloader fileDownloader, TimeProvider timeProvider) : IDataSetDownloader
{
    private const string BaseUrl = "https://www.ncei.noaa.gov/data/noaa-global-surface-temperature/v6/access/timeseries/aravg.mon.{0}.v6.0.0.{1:yyyyMM}.asc";

    // NOAA typically publishes a month's release with a delay of four to eight weeks, so the
    // current calendar month's file is often not yet present; probe backwards until one is.
    private const int MonthsToProbe = 6;

    private readonly DataSetHttpFileDownloader fileDownloader = fileDownloader;
    private readonly TimeProvider timeProvider = timeProvider;

    public string Key => "noaa-global-temperature";

    public async Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryDirectory);

        var areaIds = request.Measurements.Select(x => x.FileFilter.Id).Distinct(StringComparer.Ordinal).ToList();
        if (areaIds.Count != 1)
        {
            throw new InvalidOperationException("A NOAAGlobalTemp source asset must resolve to exactly one area.");
        }

        var areaId = areaIds[0];
        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        var candidateMonth = new DateTime(timeProvider.GetUtcNow().UtcDateTime.Year, timeProvider.GetUtcNow().UtcDateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < MonthsToProbe; i++)
        {
            var releaseUrl = string.Format(CultureInfo.InvariantCulture, BaseUrl, areaId, candidateMonth);
            try
            {
                await fileDownloader.DownloadAsync(releaseUrl, candidatePath, cancellationToken);
                return new DataSetDownloadArtifact(candidatePath);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidDataException)
            {
                if (File.Exists(candidatePath))
                {
                    File.Delete(candidatePath);
                }

                candidateMonth = candidateMonth.AddMonths(-1);
            }
        }

        throw new InvalidDataException($"No NOAAGlobalTemp release was found for area '{areaId}' in the last {MonthsToProbe} months.");
    }
}
