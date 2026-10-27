namespace ClimateExplorer.Data.Downloading.Orchestration;

using System.Collections.Concurrent;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Downloading.Workspace;
using Microsoft.Extensions.Logging;

public sealed class DataSetSourceUpdateCoordinator(
    DataSetSourceAssetResolver assetResolver,
    DataSetFreshnessPolicy freshnessPolicy,
    DataSetAssetLockProvider lockProvider,
    DataSetDownloadWorkspaceFactory workspaceFactory,
    DataSetSourceFileStore sourceFileStore,
    IDataSetSourceStateStore stateStore,
    DataSetDownloadValidator validator,
    IEnumerable<IDataSetDownloader> downloaders,
    TimeProvider timeProvider,
    ILogger<DataSetSourceUpdateCoordinator> logger,
    TimeSpan? onRequestRefreshTimeout = null) : IDataSetSourceUpdateCoordinator
{
    /// <summary>
    /// How long <see cref="PrepareAsync"/> spends bringing sources current - waiting on another request's
    /// download of the same asset included - before giving up and falling back to the existing source. Kept
    /// well under the web client's 100 second <c>HttpClient.Timeout</c> so a slow upstream (e.g. BOM) surfaces
    /// as a RefreshFailed response rather than the client abandoning the request outright.
    /// </summary>
    public static readonly TimeSpan DefaultOnRequestRefreshTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long an asset whose on-request refresh failed is left alone before another request may try again.
    /// Without it every request (and every series on a page) repeats the same failing download, each waiting up to
    /// <see cref="DefaultOnRequestRefreshTimeout"/>.
    /// </summary>
    public static readonly TimeSpan RefreshFailureBackoff = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How long after publishing a source this instance trusts its own record of it without re-reading the file.
    /// App Service's shared storage can return the replaced file for a few seconds after a publish, so the hash
    /// check would fail and a request that waited on the refresh (e.g. the panel's TempMin behind its TempMax)
    /// would download the same data again. Re-reading guards against the file being replaced behind the record -
    /// a deploy restoring the committed copy - and a deploy restarts the app, which clears this.
    /// </summary>
    public static readonly TimeSpan RecentlyPublishedTrustWindow = TimeSpan.FromMinutes(1);
    private readonly IReadOnlyDictionary<string, IDataSetDownloader> downloaders = downloaders.ToDictionary(x => x.Key, StringComparer.Ordinal);
    private readonly TimeSpan onRequestRefreshTimeout = onRequestRefreshTimeout ?? DefaultOnRequestRefreshTimeout;
    private readonly ConcurrentDictionary<string, DateTimeOffset> refreshFailedUntil = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (string Sha256, DateTimeOffset PublishedAt)> recentlyPublished = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether a downloader is registered for the given key. Used by <see cref="DataSetBatchRefresher"/> to
    /// skip assets whose downloader was deliberately left out of the registered set (e.g. when running the
    /// Data.Misc batch tool against only a subset of datasets), as distinct from a download that fails after
    /// being attempted.
    /// </summary>
    public bool IsDownloaderRegistered(string downloaderKey)
    {
        return downloaders.ContainsKey(downloaderKey);
    }

    public async Task<DataSetSourcePreparationResult> PrepareAsync(
        PostDataSetsRequestBody request,
        ICachedData? cachedData,
        bool permitSourceUpdate,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DataSetDownloadRequest> assets;
        try
        {
            assets = await assetResolver.ResolveAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve dataset source assets for request");
            return new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed);
        }

        if (assets.Count == 0)
        {
            var unmanagedOutcome = cachedData == null
                ? DataSetSourcePreparationOutcome.Rebuild
                : DataSetSourcePreparationOutcome.UseCached;
            return new DataSetSourcePreparationResult(unmanagedOutcome);
        }

        var currentStates = await GetCurrentStatesAsync(assets, cancellationToken);
        var responseIsFresh = cachedData != null &&
            assets.SelectMany(x => x.Measurements)
                .All(x => freshnessPolicy.IsFresh(cachedData, x.MeasurementDefinition.DataResolution));
        if (responseIsFresh && currentStates != null)
        {
            logger.LogDebug("Cached response is fresh for assets [{AssetKeys}]; no download attempted", string.Join(", ", assets.Select(x => x.AssetKey)));
            return new DataSetSourcePreparationResult(
                DataSetSourcePreparationOutcome.UseCached,
                DataSetRetrievalDate.OldestFor(currentStates),
                OldestLatestRecordDate(currentStates));
        }

        if (currentStates == null && permitSourceUpdate && IsBackingOff(assets))
        {
            logger.LogDebug(
                "Skipping refresh of assets [{AssetKeys}]; a recent refresh failed",
                string.Join(", ", assets.Select(x => x.AssetKey)));
            return new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed);
        }

        // Only the caller's own cancellation propagates. Hitting the refresh timeout cancels the linked token,
        // which RefreshAsync and GetCurrentStateAsync rethrow, and is treated here as an ordinary refresh failure.
        IReadOnlyList<DataSetSourceState>? refreshedStates;
        using (var refreshTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            refreshTimeout.CancelAfter(onRequestRefreshTimeout);
            try
            {
                refreshedStates = await EnsureCurrentAsync(assets, forceRefresh: false, permitSourceUpdate, refreshTimeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Refresh of assets [{AssetKeys}] did not complete within {Timeout}",
                    string.Join(", ", assets.Select(x => x.AssetKey)),
                    onRequestRefreshTimeout);
                refreshedStates = null;
            }
        }

        if (refreshedStates == null)
        {
            if (permitSourceUpdate)
            {
                var retryAfter = timeProvider.GetUtcNow().Add(RefreshFailureBackoff);
                foreach (var asset in assets)
                {
                    refreshFailedUntil[asset.AssetKey] = retryAfter;
                }
            }

            logger.LogWarning(
                "Refresh failed for one or more of assets [{AssetKeys}]; falling back to {Fallback}",
                string.Join(", ", assets.Select(x => x.AssetKey)),
                cachedData != null ? "the cached response" : "the existing published source file");
            return new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed);
        }

        return new DataSetSourcePreparationResult(
            DataSetSourcePreparationOutcome.Rebuild,
            DataSetRetrievalDate.OldestFor(refreshedStates),
            OldestLatestRecordDate(refreshedStates));
    }

    public async Task<IReadOnlyList<DataSetSourceState>?> EnsureCurrentAsync(
        IReadOnlyList<DataSetDownloadRequest> assets,
        bool forceRefresh,
        bool permitSourceUpdate,
        CancellationToken cancellationToken)
    {
        var states = new List<DataSetSourceState>();
        foreach (var asset in assets)
        {
            var state = forceRefresh ? null : await GetCurrentStateAsync(asset, cancellationToken);
            if (state == null)
            {
                state = await RefreshAsync(asset, forceRefresh, permitSourceUpdate, cancellationToken);
            }

            if (state == null)
            {
                return null;
            }

            states.Add(state);
        }

        return states;
    }

    private static DateOnly? OldestLatestRecordDate(IReadOnlyList<DataSetSourceState> states)
    {
        return states.Count == 0 || states.Any(x => x.LatestRecordDate == null)
            ? null
            : states.Min(x => x.LatestRecordDate);
    }

    private bool IsRecentlyPublished(DataSetSourceState state)
    {
        return recentlyPublished.TryGetValue(state.AssetKey, out var published) &&
            string.Equals(published.Sha256, state.Sha256, StringComparison.Ordinal) &&
            timeProvider.GetUtcNow() - published.PublishedAt < RecentlyPublishedTrustWindow;
    }

    private bool IsBackingOff(IReadOnlyList<DataSetDownloadRequest> assets)
    {
        var now = timeProvider.GetUtcNow();
        return assets.Any(x => refreshFailedUntil.TryGetValue(x.AssetKey, out var retryAfter) && retryAfter > now);
    }

    private async Task<IReadOnlyList<DataSetSourceState>?> GetCurrentStatesAsync(
        IReadOnlyList<DataSetDownloadRequest> assets,
        CancellationToken cancellationToken)
    {
        var states = new List<DataSetSourceState>();
        foreach (var asset in assets)
        {
            var state = await GetCurrentStateAsync(asset, cancellationToken);
            if (state == null)
            {
                return null;
            }

            states.Add(state);
        }

        return states;
    }

    private async Task<DataSetSourceState?> GetCurrentStateAsync(
        DataSetDownloadRequest asset,
        CancellationToken cancellationToken)
    {
        try
        {
            var state = await stateStore.GetAsync(asset.AssetKey, cancellationToken);
            if (state == null ||
                !string.Equals(state.AssetKey, asset.AssetKey, StringComparison.Ordinal) ||
                !string.Equals(state.RelativePath, asset.RelativePath, StringComparison.OrdinalIgnoreCase) ||
                !asset.Measurements.All(x => freshnessPolicy.IsFresh(state, x.MeasurementDefinition.DataResolution, asset.DownloaderKey)))
            {
                return null;
            }

            if (IsRecentlyPublished(state))
            {
                return state;
            }

            var fileInfo = await sourceFileStore.GetFileInfoAsync(asset.RelativePath, cancellationToken);
            return fileInfo != null && fileInfo.Length == state.Length && fileInfo.Sha256 == state.Sha256
                ? state
                : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read current dataset source state for asset {AssetKey}", asset.AssetKey);
            return null;
        }
    }

    private async Task<DataSetSourceState?> RefreshAsync(
        DataSetDownloadRequest asset,
        bool forceRefresh,
        bool permitSourceUpdate,
        CancellationToken cancellationToken)
    {
        if (!permitSourceUpdate)
        {
            return null;
        }

        await using var lease = await lockProvider.AcquireAsync(asset.AssetKey, cancellationToken);
        if (!forceRefresh)
        {
            var stateAfterLock = await GetCurrentStateAsync(asset, cancellationToken);
            if (stateAfterLock != null)
            {
                return stateAfterLock;
            }

            var adoptedState = await TryAdoptPublishedSourceAsync(asset, cancellationToken);
            if (adoptedState != null)
            {
                return adoptedState;
            }
        }

        if (!downloaders.TryGetValue(asset.DownloaderKey, out var downloader))
        {
            logger.LogWarning("No downloader registered for key {DownloaderKey}, asset {AssetKey}", asset.DownloaderKey, asset.AssetKey);
            return null;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var workspace = workspaceFactory.Create();
            var artifact = await downloader.DownloadAsync(asset, workspace.Path, cancellationToken);
            var latestRecordDate = await validator.ValidateAsync(asset, workspace.Path, cancellationToken);

            // Hash the candidate rather than reading the published file back: on App Service's shared storage a
            // read straight after the publish has returned the replaced file, recording its hash against this
            // download's latest record date. A later deploy restoring that file then made it look current.
            var fileInfo = await DataSetSourceFileStore.ComputeFileInfoAsync(artifact.CandidateFilePath, cancellationToken);
            await sourceFileStore.PublishAsync(artifact.CandidateFilePath, asset.RelativePath, cancellationToken);
            var state = new DataSetSourceState
            {
                AssetKey = asset.AssetKey,
                RelativePath = asset.RelativePath,
                Length = fileInfo.Length,
                Sha256 = fileInfo.Sha256,
                RetrievedDate = timeProvider.GetUtcNow(),
                LatestRecordDate = latestRecordDate,
            };
            await stateStore.PutAsync(state, cancellationToken);
            recentlyPublished[asset.AssetKey] = (state.Sha256, timeProvider.GetUtcNow());
            logger.LogInformation(
                "Refreshed dataset source {AssetKey} via {DownloaderKey} in {ElapsedMs}ms; published {Length} bytes, latest record date {LatestRecordDate}",
                asset.AssetKey,
                asset.DownloaderKey,
                stopwatch.ElapsedMilliseconds,
                fileInfo.Length,
                latestRecordDate);
            return state;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to refresh dataset source {AssetKey} via {DownloaderKey} after {ElapsedMs}ms; retaining previously published source",
                asset.AssetKey,
                asset.DownloaderKey,
                stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    /// <summary>
    /// The state store lives in per-instance temporary storage, so an app restart or a new instance loses it
    /// while the published file it described is still on disk. When that file's own content already proves
    /// it current (a content-aware daily source holding yesterday's record), record a state for it as-is
    /// rather than downloading what is already there. Anything less is left to the ordinary download.
    /// </summary>
    private async Task<DataSetSourceState?> TryAdoptPublishedSourceAsync(
        DataSetDownloadRequest asset,
        CancellationToken cancellationToken)
    {
        if (!asset.Measurements.All(x => freshnessPolicy.CanBeCurrentByContent(x.MeasurementDefinition.DataResolution, asset.DownloaderKey)))
        {
            return null;
        }

        try
        {
            var publishedPath = sourceFileStore.ResolvePath(asset.RelativePath);
            if (!File.Exists(publishedPath))
            {
                return null;
            }

            // Check and hash one copy of the file, so the record date and the hash recorded together always describe
            // the same content - separate reads of the published file can see different versions of it while another
            // instance publishes over it, or while shared storage still returns the version it replaced.
            using var workspace = workspaceFactory.Create();
            var snapshotPath = DataSetDownloadPath.Resolve(workspace.Path, asset.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            var publishedLastWriteTimeUtc = File.GetLastWriteTimeUtc(publishedPath);
            File.Copy(publishedPath, snapshotPath);

            var latestRecordDate = await validator.ValidateAsync(asset, workspace.Path, cancellationToken);
            if (!freshnessPolicy.HasYesterdaysRecord(latestRecordDate))
            {
                return null;
            }

            var fileInfo = await DataSetSourceFileStore.ComputeFileInfoAsync(snapshotPath, cancellationToken);
            var state = new DataSetSourceState
            {
                AssetKey = asset.AssetKey,
                RelativePath = asset.RelativePath,
                Length = fileInfo.Length,
                Sha256 = fileInfo.Sha256,
                RetrievedDate = publishedLastWriteTimeUtc,
                LatestRecordDate = latestRecordDate,
            };
            await stateStore.PutAsync(state, cancellationToken);
            logger.LogInformation(
                "Adopted already published dataset source {AssetKey} without downloading; latest record date {LatestRecordDate}",
                asset.AssetKey,
                latestRecordDate);
            return state;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to adopt published dataset source {AssetKey}; downloading instead", asset.AssetKey);
            return null;
        }
    }
}
