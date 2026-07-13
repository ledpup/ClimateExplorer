namespace ClimateExplorer.Data.Ghcnd;

using System.Net.Http;

public static class GhcndStationCsvDownloader
{
    public static string GetDownloadUrl(string stationId)
    {
        return $"https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/{stationId}.csv";
    }

    public static async Task<string> DownloadCsvAsync(
        HttpClient httpClient,
        string stationId,
        CancellationToken cancellationToken = default)
    {
        var url = GetDownloadUrl(stationId);
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred while downloading the CSV for station {stationId} from {url}.", ex);
        }
    }
}
