namespace ClimateExplorer.WebApi;

using System.Net.Http;
using ClimateExplorer.WebApi.Infrastructure;

public sealed class ClimateExplorerApiServices
{
    public ClimateExplorerApiServices(
        ICache cache,
        ICache longtermCache,
        HttpClient bomHttpClient,
        HttpClient ghcndHttpClient)
    {
        Cache = cache;
        LongtermCache = longtermCache;
        BomHttpClient = bomHttpClient;
        GhcndHttpClient = ghcndHttpClient;
    }

    public ICache Cache { get; }

    public ICache LongtermCache { get; }

    public HttpClient BomHttpClient { get; }

    public HttpClient GhcndHttpClient { get; }
}
