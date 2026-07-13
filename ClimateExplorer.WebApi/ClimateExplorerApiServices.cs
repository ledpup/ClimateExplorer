namespace ClimateExplorer.WebApi;

using System.Net.Http;
using ClimateExplorer.Data.Downloading;
using ClimateExplorer.WebApi.Infrastructure;

public sealed class ClimateExplorerApiServices(
    ICache cache,
    ICache longtermCache,
    HttpClient bomHttpClient,
    HttpClient ghcndHttpClient)
{
    internal ClimateExplorerApiServices(
        ICache cache,
        ICache longtermCache,
        HttpClient bomHttpClient,
        HttpClient ghcndHttpClient,
        IDataSetSourceUpdateCoordinator dataSetSourceUpdateCoordinator)
        : this(cache, longtermCache, bomHttpClient, ghcndHttpClient)
    {
        DataSetSourceUpdateCoordinator = dataSetSourceUpdateCoordinator;
    }

    public ICache Cache { get; } = cache;

    public ICache LongtermCache { get; } = longtermCache;

    public HttpClient BomHttpClient { get; } = bomHttpClient;

    public HttpClient GhcndHttpClient { get; } = ghcndHttpClient;

    internal IDataSetSourceUpdateCoordinator DataSetSourceUpdateCoordinator { get; private set; } = null!;
}
