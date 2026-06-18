namespace ClimateExplorer.Data.Ghcnd;

using System.Net.Http;
using ClimateExplorer.Core.Model;
using Microsoft.Extensions.Logging;

public static class GhcndBulkStationFileCache
{
    public static async Task DownloadStationsAsync(List<Station> stations, HttpClient httpClient, string downloadFolder, ILogger logger, int maxDegreeOfParallelism = 5)
    {
        Directory.CreateDirectory(downloadFolder);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(stations, parallelOptions, async (station, token) =>
        {
            var csvFilePathAndName = GetCsvFilePathAndName(downloadFolder, station.Id);

            if (File.Exists(csvFilePathAndName))
            {
                logger.LogInformation($"GHCNd file for {station.Id} ({station.Name}) already exists ({csvFilePathAndName}). Will not download it again.");
                return;
            }

            logger.LogInformation($"Downloading GHCNd for {station.Id}");
            var content = await GhcndStationCsvDownloader.DownloadCsvAsync(httpClient, station.Id);
            await File.WriteAllTextAsync(csvFilePathAndName, content);
            logger.LogInformation($"Downloaded GHCNd for {station.Id} ({station.Name}) in {station.CountryCode}");
        });
    }

    public static string GetCsvFilePathAndName(string downloadFolder, string stationId)
    {
        return @$"{downloadFolder}\{stationId}.csv";
    }
}
