namespace ClimateExplorer.WebApi.DataRetrieval;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

internal sealed class DataSetAssetLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.Ordinal);

    public async ValueTask<IAsyncDisposable> AcquireAsync(string assetKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetKey);
        var semaphore = locks.GetOrAdd(assetKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        private SemaphoreSlim semaphore = semaphore;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref semaphore, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
