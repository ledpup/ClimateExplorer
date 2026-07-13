namespace ClimateExplorer.Data.Downloading;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;

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

    public DataSetSourceUpdateCoordinator(
        DataSetSourceAssetResolver assetResolver,
        DataSetFreshnessPolicy freshnessPolicy,
        DataSetAssetLockProvider lockProvider,
        DataSetDownloadWorkspaceFactory workspaceFactory,
        DataSetSourceFileStore sourceFileStore,
        IDataSetSourceStateStore stateStore,
        DataSetDownloadValidator validator,
        IEnumerable<IDataSetDownloader> downloaders,
        TimeProvider timeProvider)
    {
        this.assetResolver = assetResolver;
        this.freshnessPolicy = freshnessPolicy;
        this.lockProvider = lockProvider;
        this.workspaceFactory = workspaceFactory;
        this.sourceFileStore = sourceFileStore;
        this.stateStore = stateStore;
        this.validator = validator;
        this.timeProvider = timeProvider;
        this.downloaders = downloaders.ToDictionary(x => x.Key, StringComparer.Ordinal);
    }

    public async Task<DataSetSourcePreparationResult> PrepareAsync(
        PostDataSetsRequestBody request,
        DataSet? cachedData,
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
        catch
        {
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
            return new DataSetSourcePreparationResult(
                DataSetSourcePreparationOutcome.UseCached,
                DataSetRetrievalDate.OldestFor(currentStates));
        }

        var refreshedStates = await EnsureCurrentAsync(assets, forceRefresh: false, cancellationToken);
        if (refreshedStates == null)
        {
            return new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed);
        }

        return new DataSetSourcePreparationResult(
            DataSetSourcePreparationOutcome.Rebuild,
            DataSetRetrievalDate.OldestFor(refreshedStates));
    }

    public async Task<IReadOnlyList<DataSetSourceState>?> EnsureCurrentAsync(
        IReadOnlyList<DataSetDownloadRequest> assets,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var states = new List<DataSetSourceState>();
        foreach (var asset in assets)
        {
            var state = forceRefresh ? null : await GetCurrentStateAsync(asset, cancellationToken);
            if (state == null)
            {
                state = await RefreshAsync(asset, forceRefresh, cancellationToken);
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
                !asset.Measurements.All(x => freshnessPolicy.IsFresh(state, x.MeasurementDefinition.DataResolution)))
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
        catch
        {
            return null;
        }
    }

    private async Task<DataSetSourceState?> RefreshAsync(
        DataSetDownloadRequest asset,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        await using var lease = await lockProvider.AcquireAsync(asset.AssetKey, cancellationToken);
        if (!forceRefresh)
        {
            var stateAfterLock = await GetCurrentStateAsync(asset, cancellationToken);
            if (stateAfterLock != null)
            {
                return stateAfterLock;
            }
        }

        try
        {
            if (!downloaders.TryGetValue(asset.DownloaderKey, out var downloader))
            {
                return null;
            }

            using var workspace = workspaceFactory.Create();
            var artifact = await downloader.DownloadAsync(asset, workspace.Path, cancellationToken);
            await validator.ValidateAsync(asset, workspace.Path, cancellationToken);
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
            };
            await stateStore.PutAsync(state, cancellationToken);
            return state;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }
}
