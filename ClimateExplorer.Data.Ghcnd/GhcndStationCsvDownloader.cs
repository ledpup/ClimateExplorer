namespace ClimateExplorer.Data.Ghcnd;

using System.Net.Http;

public static class GhcndStationCsvDownloader
{
    public static string GetDownloadUrl(string stationId)
    {
        return $"https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/{stationId}.csv";
    }

    public static async Task<string> DownloadCsvAsync(HttpClient httpClient, string stationId)
    {
        var url = GetDownloadUrl(stationId);
        var response = await httpClient.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}
