namespace ClimateExplorer.Data.Downloading.Orchestration;

using System.Collections.Concurrent;
using ClimateExplorer.Data.Downloading.Models;
using Microsoft.Extensions.Logging;

public sealed class DataSetBatchRefresher(
    DataSetSourceAssetResolver assetResolver,
    DataSetSourceUpdateCoordinator coordinator,
    ILogger<DataSetBatchRefresher>? logger = null,
    int maxDegreeOfParallelism = 5) : IDataSetBatchRefresher
{
    private readonly DataSetSourceAssetResolver assetResolver = assetResolver;
    private readonly DataSetSourceUpdateCoordinator coordinator = coordinator;
    private readonly ILogger<DataSetBatchRefresher>? logger = logger;
    private readonly int maxDegreeOfParallelism = maxDegreeOfParallelism;

    public async Task RefreshAllAsync(bool forceRefresh = true, CancellationToken cancellationToken = default)
    {
        var allAssets = await assetResolver.ResolveAllAsync(cancellationToken);

        // Assets whose downloader key isn't registered are deliberately excluded from this run (e.g. the
        // Data.Misc tool was invoked with only a subset of downloaders registered) rather than treated as
        // failures - that mirrors the old behaviour of commenting out a downloader to skip its dataset.
        var assets = new List<DataSetDownloadRequest>();
        foreach (var asset in allAssets)
        {
            if (coordinator.IsDownloaderRegistered(asset.DownloaderKey))
            {
                assets.Add(asset);
            }
            else
            {
                logger?.LogInformation("Skipping dataset source asset {AssetKey}: no downloader registered for key {DownloaderKey}", asset.AssetKey, asset.DownloaderKey);
            }
        }

        var failures = new ConcurrentBag<string>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(
            assets,
            parallelOptions,
            async (asset, token) =>
            {
                var states = await coordinator.EnsureCurrentAsync([asset], forceRefresh, permitSourceUpdate: true, token);
                if (states == null)
                {
                    failures.Add(asset.AssetKey);
                }
            });

        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"Failed to refresh {failures.Count} dataset source asset(s): {string.Join(", ", failures)}");
        }
    }
}
