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
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred while downloading the CSV for station {stationId} from {url}.", ex);
        }
    }
}
