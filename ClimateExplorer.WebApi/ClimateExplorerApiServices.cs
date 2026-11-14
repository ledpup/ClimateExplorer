namespace ClimateExplorer.WebApi;

using System.Net.Http;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class ClimateExplorerApiServices(
    ICache cache,
    ICache longtermCache,
    HttpClient ghcndHttpClient)
{
    internal ClimateExplorerApiServices(
        ICache cache,
        ICache longtermCache,
        HttpClient ghcndHttpClient,
        IDataSetSourceUpdateCoordinator dataSetSourceUpdateCoordinator)
        : this(cache, longtermCache, ghcndHttpClient)
    {
        DataSetSourceUpdateCoordinator = dataSetSourceUpdateCoordinator;
    }

    internal ClimateExplorerApiServices(
        ICache cache,
        ICache longtermCache,
        HttpClient ghcndHttpClient,
        IDataSetSourceUpdateCoordinator dataSetSourceUpdateCoordinator,
        ILogger<ClimateExplorerApiServices> logger)
        : this(cache, longtermCache, ghcndHttpClient, dataSetSourceUpdateCoordinator)
    {
        Logger = logger;
    }

    public ICache Cache { get; } = cache;

    public ICache LongtermCache { get; } = longtermCache;

    public HttpClient GhcndHttpClient { get; } = ghcndHttpClient;

    internal IDataSetSourceUpdateCoordinator DataSetSourceUpdateCoordinator { get; private set; } = null!;

    internal ILogger<ClimateExplorerApiServices> Logger { get; private set; } = NullLogger<ClimateExplorerApiServices>.Instance;

    /// <summary>
    /// The long-term cache's "Locations" entry, kept in memory. The file is several MB and every location lookup
    /// (by Id, by path, nearby) needs it; it only changes when this process rebuilds it.
    /// </summary>
    internal Location[] Locations { get; set; }
}
