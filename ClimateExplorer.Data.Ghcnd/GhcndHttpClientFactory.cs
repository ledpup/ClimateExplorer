namespace ClimateExplorer.Data.Ghcnd;

using System.Net.Http;

public static class GhcndHttpClientFactory
{
    public static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
        var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
        return httpClient;
    }
}
