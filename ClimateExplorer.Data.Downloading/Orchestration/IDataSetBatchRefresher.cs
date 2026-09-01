namespace ClimateExplorer.Data.Downloading.Orchestration;

public interface IDataSetBatchRefresher
{
    /// <summary>
    /// Refreshes every opted-in dataset source asset.
    /// </summary>
    /// <param name="forceRefresh">
    /// When true (the default), re-downloads every asset regardless of current freshness - see
    /// docs/operations/automated-dataset-downloads.md's "Forcing a refresh" section. Pass false to reuse a
    /// still-fresh previously downloaded source instead (per <see cref="DataSetFreshnessPolicy"/>), e.g. for
    /// repeated local runs while iterating on a downloader/transformer without re-pulling a large source file
    /// each time.
    /// </param>
    Task RefreshAllAsync(bool forceRefresh = true, CancellationToken cancellationToken = default);
}
