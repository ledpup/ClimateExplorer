namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Downloading.Workspace;
using Microsoft.Extensions.Logging;

public sealed class DataSetSourceUpdateCoordinator : IDataSetSourceUpdateCoordinator
{
    private readonly DataSetSourceAssetResolver assetResolver;
    private readonly DataSetFreshnessPolicy freshnessPolicy;
    private readonly DataSetAssetLockProvider lockProvider;
    private readonly DataSetDownloadWorkspaceFactory workspaceFactory;
    private readonly DataSetSourceFileStore sourceFileStore;
    private readonly IDataSetSourceStateStore stateStore;
    private readonly DataSetDownloadValidator validator;
    private readonly IReadOnlyDictionary<string, IDataSetDownloader> downloaders;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<DataSetSourceUpdateCoordinator> logger;

    public DataSetSourceUpdateCoordinator(
        DataSetSourceAssetResolver assetResolver,
        DataSetFreshnessPolicy freshnessPolicy,
        DataSetAssetLockProvider lockProvider,
        DataSetDownloadWorkspaceFactory workspaceFactory,
        DataSetSourceFileStore sourceFileStore,
        IDataSetSourceStateStore stateStore,
        DataSetDownloadValidator validator,
        IEnumerable<IDataSetDownloader> downloaders,
        TimeProvider timeProvider,
        ILogger<DataSetSourceUpdateCoordinator> logger)
    {
        this.assetResolver = assetResolver;
        this.freshnessPolicy = freshnessPolicy;
        this.lockProvider = lockProvider;
        this.workspaceFactory = workspaceFactory;
        this.sourceFileStore = sourceFileStore;
        this.stateStore = stateStore;
        this.validator = validator;
        this.timeProvider = timeProvider;
        this.logger = logger;
        this.downloaders = downloaders.ToDictionary(x => x.Key, StringComparer.Ordinal);
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
                DataSetRetrievalDate.OldestFor(currentStates));
        }

        var refreshedStates = await EnsureCurrentAsync(assets, forceRefresh: false, permitSourceUpdate, cancellationToken);
        if (refreshedStates == null)
        {
            logger.LogWarning(
                "Refresh failed for one or more of assets [{AssetKeys}]; falling back to {Fallback}",
                string.Join(", ", assets.Select(x => x.AssetKey)),
                cachedData != null ? "the cached response" : "the existing published source file");
            return new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed);
        }

        return new DataSetSourcePreparationResult(
            DataSetSourcePreparationOutcome.Rebuild,
            DataSetRetrievalDate.OldestFor(refreshedStates));
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
            await sourceFileStore.PublishAsync(artifact.CandidateFilePath, asset.RelativePath, cancellationToken);
            var fileInfo = await sourceFileStore.GetFileInfoAsync(asset.RelativePath, cancellationToken)
                ?? throw new FileNotFoundException("Published dataset source file was not found.");
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
}
