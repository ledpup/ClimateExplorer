namespace ClimateExplorer.WebApi;

using System.Net.Http;
using ClimateExplorer.WebApi.Infrastructure;

public sealed class ClimateExplorerApiServices(
    ICache cache,
    ICache longtermCache,
    HttpClient bomHttpClient,
    HttpClient ghcndHttpClient)
{
    public ICache Cache { get; } = cache;

    public ICache LongtermCache { get; } = longtermCache;

    public HttpClient BomHttpClient { get; } = bomHttpClient;

    public HttpClient GhcndHttpClient { get; } = ghcndHttpClient;
}
