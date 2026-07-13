namespace ClimateExplorer.Data.Downloading;

public sealed class DataSetBatchRefresher(
    DataSetSourceAssetResolver assetResolver,
    DataSetSourceUpdateCoordinator coordinator) : IDataSetBatchRefresher
{
    private readonly DataSetSourceAssetResolver assetResolver = assetResolver;
    private readonly DataSetSourceUpdateCoordinator coordinator = coordinator;

    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        var assets = await assetResolver.ResolveAllAsync(cancellationToken);
        var failures = new List<string>();

        foreach (var asset in assets)
        {
            var states = await coordinator.EnsureCurrentAsync([asset], forceRefresh: true, cancellationToken);
            if (states == null)
            {
                failures.Add(asset.AssetKey);
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"Failed to refresh {failures.Count} dataset source asset(s): {string.Join(", ", failures)}");
        }
    }
}
