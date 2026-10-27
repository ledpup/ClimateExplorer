#nullable enable
namespace ClimateExplorer.WebApi;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Read-only view of how this instance sees a location's automatically refreshed source files: where it
/// resolves each file, what is on disk there, and the refresh state it has recorded for it. Added to
/// diagnose stale data that the freshness check reported as current; it never downloads or writes anything.
/// </summary>
internal static class DiagnosticsEndpoints
{
    // Matches the folder DataReaderFunctions reads from by default.
    private const string ReaderDatasetsFolder = "Datasets";

    public static async Task<SourceStateDiagnostics> GetSourceState(
        Guid locationId,
        [FromServices] DataSetSourceAssetResolver assetResolver,
        [FromServices] DataSetSourceFileStore sourceFileStore,
        [FromServices] IDataSetSourceStateStore stateStore,
        [FromServices] DataSetFreshnessPolicy freshnessPolicy,
        CancellationToken cancellationToken)
    {
        var assets = (await assetResolver.ResolveAllAsync(cancellationToken))
            .Where(asset => asset.Measurements.Any(x => IsMappedToLocation(asset.DataSetDefinition, x.FileFilter.Id, locationId)))
            .ToList();

        var assetDiagnostics = new List<SourceAssetDiagnostics>();
        foreach (var asset in assets)
        {
            assetDiagnostics.Add(await GetAssetDiagnostics(asset, sourceFileStore, stateStore, freshnessPolicy, cancellationToken));
        }

        return new SourceStateDiagnostics(
            DateTimeOffset.UtcNow,
            Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"),
            Environment.MachineName,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.GetTempPath(),
            sourceFileStore.RootPath,
            Path.GetFullPath(ReaderDatasetsFolder),
            Environment.GetEnvironmentVariable("WEBSITE_RUN_FROM_PACKAGE"),
            Environment.GetEnvironmentVariable("WEBSITE_LOCAL_CACHE_OPTION"),
            assetDiagnostics);
    }

    private static async Task<SourceAssetDiagnostics> GetAssetDiagnostics(
        DataSetDownloadRequest asset,
        DataSetSourceFileStore sourceFileStore,
        IDataSetSourceStateStore stateStore,
        DataSetFreshnessPolicy freshnessPolicy,
        CancellationToken cancellationToken)
    {
        var storeFilePath = sourceFileStore.ResolvePath(asset.RelativePath);
        var readerFilePath = Path.GetFullPath(Path.Combine(Path.GetFullPath(ReaderDatasetsFolder), asset.RelativePath));

        SourceFileDiagnostics? file = null;
        string? fileError = null;
        try
        {
            var fileInfo = await sourceFileStore.GetFileInfoAsync(asset.RelativePath, cancellationToken);
            if (fileInfo != null)
            {
                file = new SourceFileDiagnostics(fileInfo.Length, fileInfo.Sha256, fileInfo.LastWriteTimeUtc);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            fileError = ex.Message;
        }

        DataSetSourceState? state = null;
        string? stateError = null;
        try
        {
            state = await stateStore.GetAsync(asset.AssetKey, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stateError = ex.Message;
        }

        return new SourceAssetDiagnostics(
            asset.AssetKey,
            asset.DataSetDefinition.ShortName,
            asset.DownloaderKey,
            [.. asset.Measurements.Select(x => $"{x.MeasurementDefinition.DataType} {x.MeasurementDefinition.DataResolution} {x.FileFilter.Id}")],
            storeFilePath,
            readerFilePath,
            file,
            fileError,
            (stateStore as FileDataSetSourceStateStore)?.GetStatePath(asset.AssetKey),
            state,
            stateError,
            state != null && asset.Measurements.All(x => freshnessPolicy.IsFresh(state, x.MeasurementDefinition.DataResolution, asset.DownloaderKey)),
            state != null && file != null && state.Length == file.Length && state.Sha256 == file.Sha256);
    }

    private static bool IsMappedToLocation(DataSetDefinition dataSet, string stationId, Guid locationId)
    {
        return dataSet.DataLocationMapping?.LocationIdToDataFileMappings.TryGetValue(locationId, out var filters) == true &&
            filters.Any(x => string.Equals(x.Id, stationId, StringComparison.Ordinal));
    }

    internal sealed record SourceStateDiagnostics(
        DateTimeOffset NowUtc,
        string? InstanceId,
        string MachineName,
        string CurrentDirectory,
        string BaseDirectory,
        string TempPath,
        string SourceFileStoreRoot,
        string ReaderDatasetsRoot,
        string? RunFromPackage,
        string? LocalCacheOption,
        IReadOnlyList<SourceAssetDiagnostics> Assets);

    internal sealed record SourceAssetDiagnostics(
        string AssetKey,
        string? DataSet,
        string DownloaderKey,
        IReadOnlyList<string> Measurements,
        string StoreFilePath,
        string ReaderFilePath,
        SourceFileDiagnostics? File,
        string? FileError,
        string? StateFilePath,
        DataSetSourceState? State,
        string? StateError,
        bool StateIsFresh,
        bool FileMatchesState);

    internal sealed record SourceFileDiagnostics(long Length, string Sha256, DateTimeOffset LastWriteTimeUtc);
}
