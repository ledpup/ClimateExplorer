namespace ClimateExplorer.WebApi.DataRetrieval;

using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading;
using ClimateExplorer.WebApi.Infrastructure;

internal sealed class CachedDataSetSourceStateStore(ICache cache) : IDataSetSourceStateStore
{
    private const string CacheKeyPrefix = "DataSetSourceState_v1_";
    private readonly ICache cache = cache;

    public Task<DataSetSourceState> GetAsync(string assetKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return cache.Get<DataSetSourceState>(CacheKeyPrefix + assetKey);
    }

    public Task PutAsync(DataSetSourceState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return cache.Put(CacheKeyPrefix + state.AssetKey, state);
    }
}
