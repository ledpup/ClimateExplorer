namespace ClimateExplorer.Data.Downloading.Orchestration;

using System.Collections.Concurrent;

public sealed class DataSetBatchRefresher(
    DataSetSourceAssetResolver assetResolver,
    DataSetSourceUpdateCoordinator coordinator,
    int maxDegreeOfParallelism = 5) : IDataSetBatchRefresher
{
    private readonly DataSetSourceAssetResolver assetResolver = assetResolver;
    private readonly DataSetSourceUpdateCoordinator coordinator = coordinator;
    private readonly int maxDegreeOfParallelism = maxDegreeOfParallelism;

    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        var assets = await assetResolver.ResolveAllAsync(cancellationToken);
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
                var states = await coordinator.EnsureCurrentAsync([asset], forceRefresh: true, permitSourceUpdate: true, token);
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
