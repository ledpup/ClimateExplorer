namespace ClimateExplorer.Data.Downloading.Storage;

using System.Collections.Concurrent;

public sealed class DataSetAssetLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.Ordinal);

    public async ValueTask<IAsyncDisposable> AcquireAsync(string assetKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetKey);
        var semaphore = locks.GetOrAdd(assetKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    /// <summary>
    /// Like <see cref="AcquireAsync"/>, but gives up after <paramref name="timeout"/> and returns null.
    /// </summary>
    public async ValueTask<IAsyncDisposable?> TryAcquireAsync(string assetKey, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetKey);
        var semaphore = locks.GetOrAdd(assetKey, _ => new SemaphoreSlim(1, 1));
        return await semaphore.WaitAsync(timeout, cancellationToken) ? new Releaser(semaphore) : null;
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        private SemaphoreSlim? semaphore = semaphore;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref semaphore, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
