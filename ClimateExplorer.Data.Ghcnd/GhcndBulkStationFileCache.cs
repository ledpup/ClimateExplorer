namespace ClimateExplorer.Data.Ghcnd;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using ClimateExplorer.Core.Model;
using Microsoft.Extensions.Logging;

public static class GhcndBulkStationFileCache
{
    /// <summary>
    /// Downloads each station's GHCNd CSV into <paramref name="downloadFolder"/>, skipping files already there.
    /// A station GHCNd doesn't have (404) is skipped, since not every GHCNm station is in GHCNd; the caller
    /// treats a missing file as "no daily data". Any other failure throws, so a network problem can't quietly
    /// drop stations.
    /// </summary>
    /// <returns>The IDs of the stations GHCNd doesn't have.</returns>
    public static async Task<IReadOnlyList<string>> DownloadStationsAsync(List<Station> stations, HttpClient httpClient, string downloadFolder, ILogger logger, int maxDegreeOfParallelism = 5)
    {
        Directory.CreateDirectory(downloadFolder);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        var notInGhcnd = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(stations, parallelOptions, async (station, token) =>
        {
            var csvFilePathAndName = GetCsvFilePathAndName(downloadFolder, station.Id);

            if (File.Exists(csvFilePathAndName))
            {
                logger.LogInformation($"GHCNd file for {station.Id} ({station.Name}) already exists ({csvFilePathAndName}). Will not download it again.");
                return;
            }

            logger.LogInformation($"Downloading GHCNd for {station.Id}");

            string content;
            try
            {
                content = await GhcndStationCsvDownloader.DownloadCsvAsync(httpClient, station.Id, token);
            }
            catch (Exception ex) when (IsNotFound(ex))
            {
                logger.LogWarning($"GHCNd has no file for {station.Id} ({station.Name}) in {station.CountryCode}. It will have GHCNm monthly data only.");
                notInGhcnd.Add(station.Id);
                return;
            }

            await File.WriteAllTextAsync(csvFilePathAndName, content, token);
            logger.LogInformation($"Downloaded GHCNd for {station.Id} ({station.Name}) in {station.CountryCode}");
        });

        if (!notInGhcnd.IsEmpty)
        {
            logger.LogWarning($"{notInGhcnd.Count} of {stations.Count} stations aren't in GHCNd: {string.Join(", ", notInGhcnd.Order())}");
        }

        return [.. notInGhcnd.Order()];
    }

    public static string GetCsvFilePathAndName(string downloadFolder, string stationId)
    {
        return Path.Combine(downloadFolder, $"{stationId}.csv");
    }

    private static bool IsNotFound(Exception ex)
    {
        return ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound }
            || ex.InnerException is HttpRequestException { StatusCode: HttpStatusCode.NotFound };
    }
}
