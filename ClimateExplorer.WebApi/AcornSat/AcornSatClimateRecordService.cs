#nullable enable
namespace ClimateExplorer.WebApi.AcornSat;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Extenders;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.WebApi.Metadata;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Detects and resolves the ACORN-SAT on-request CDO extension: coordinates CDO freshness through the
/// existing <see cref="IDataSetSourceUpdateCoordinator"/>, invokes the pure
/// <see cref="AcornSatRecordExtender"/> against freshly read ACORN-SAT/CDO series, and maintains the small
/// <see cref="AcornSatExtensionCache"/> entry. Never mutates <c>ACORN-SAT.zip</c> or its source files.
/// </summary>
internal sealed class AcornSatClimateRecordService(
    IDataSetSourceUpdateCoordinator coordinator,
    AcornSatExtensionCache extensionCache,
    DataSetAssetLockProvider lockProvider,
    TimeProvider timeProvider,
    ILogger<AcornSatClimateRecordService> logger)
{
    public async Task<AcornSatExtensionOutcome> ResolveAsync(Guid locationId, DataType dataType, CancellationToken cancellationToken)
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions();
        var acornSatDataSet = definitions.Single(x => x.Id == AcornSatDatasetIds.AcornSat);
        var cdoDataSet = definitions.Single(x => x.Id == AcornSatDatasetIds.Cdo);

        var stations = AcornSatStationResolver.Resolve(acornSatDataSet, cdoDataSet, locationId);
        if (!stations.IsResolved)
        {
            logger.LogDebug(
                "ACORN-SAT extension not eligible for location {LocationId}/{DataType}: no single open CDO station",
                locationId,
                dataType);
            return AcornSatExtensionOutcome.NotEligible(AcornSatExtensionDecision.NoOpenCdoStation, stations);
        }

        var extensionKey = $"acorn-sat-extension:{locationId:D}:{dataType}";
        await using var extensionLease = await lockProvider.AcquireAsync(extensionKey, cancellationToken);

        var cachedEntry = await extensionCache.GetAsync(locationId, dataType);
        var cdoRequest = BuildCdoRequest(cdoDataSet.Id, locationId, dataType);

        var preparation = await coordinator.PrepareAsync(cdoRequest, cachedEntry, permitSourceUpdate: true, cancellationToken);

        if (preparation.Outcome == DataSetSourcePreparationOutcome.RefreshFailed)
        {
            logger.LogWarning(
                "CDO refresh failed while resolving the ACORN-SAT extension for {LocationId}/{DataType}; " +
                "falling back to a cached overlay when one exists, or the base ACORN-SAT series otherwise",
                locationId,
                dataType);

            if (cachedEntry is { IsConclusive: true })
            {
                return AcornSatExtensionOutcome.FromCacheEntry(cachedEntry);
            }

            // No usable cached overlay: fall through and attempt the comparison against whatever CDO archive
            // is already published (which may not exist at all), matching DataSetEndpoints' cold-source
            // fallback. The resulting retrieval time is null and therefore remains retryable.
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var acornSatSeries = await ReadSeries(acornSatDataSet.Id, locationId, dataType, DataAdjustment.Adjusted);
        var cdoSeries = await ReadSeries(cdoDataSet.Id, locationId, dataType, DataAdjustment.Unadjusted);

        var extension = AcornSatRecordExtender.Extend(
            acornSatSeries.DataRecords,
            cdoSeries.DataRecords,
            stations.AdjustedStationId,
            stations.OpenCdoStationId,
            today);

        var retrievedDate = preparation.Outcome == DataSetSourcePreparationOutcome.RefreshFailed
            ? null
            : preparation.RetrievedDate;

        if (extension.Decision is AcornSatExtensionDecision.Eligible or AcornSatExtensionDecision.AdjustmentsDetected)
        {
            var entry = new AcornSatExtensionCacheEntry
            {
                LocationId = locationId,
                DataType = dataType,
                AdjustedStationId = stations.AdjustedStationId!,
                OpenCdoStationId = stations.OpenCdoStationId!,
                ComparisonYear = extension.ComparisonYear!.Value,
                Decision = extension.Decision,
                LatestAcornSatDate = extension.LatestAcornSatDate!.Value,
                ComparisonSignature = extension.ComparisonSignature!,
                OverlayRecords = extension.OverlayRecords.ToList(),
                RetrievedDate = retrievedDate,
            };
            await extensionCache.PutAsync(entry);
        }

        logger.LogInformation(
            "ACORN-SAT extension decision for {LocationId}/{DataType}: {Decision} (comparison year {ComparisonYear}, {OverlayCount} overlay records)",
            locationId,
            dataType,
            extension.Decision,
            extension.ComparisonYear,
            extension.OverlayRecords.Count);

        return new AcornSatExtensionOutcome(extension, retrievedDate);
    }

    /// <summary>
    /// Composes the adjusted ACORN-SAT series with the CDO overlay (if any) and builds the ordinary
    /// <see cref="DataSet"/> response from it via <see cref="DataSetEndpoints.BuildResponseDataSet"/>, so
    /// <c>/climate-record</c> sees the same binning, filtering, and pagination pipeline as every other
    /// request. Appends CDO source metadata only when the overlay actually contributed a value.
    /// </summary>
    public async Task<DataSet> BuildComposedDataSetAsync(PostDataSetsRequestBody body, CancellationToken cancellationToken)
    {
        var spec = body.SeriesSpecifications!.Single();
        var outcome = await ResolveAsync(spec.LocationId, spec.DataType, cancellationToken);

        var acornSatSeries = await ReadSeries(AcornSatDatasetIds.AcornSat, spec.LocationId, spec.DataType, DataAdjustment.Adjusted);
        var composedRecords = acornSatSeries.DataRecords!
            .Concat(outcome.Extension.OverlayRecords)
            .OrderBy(x => x.Date)
            .ToArray();
        var composedSeries = new SeriesProvider.Series
        {
            DataRecords = composedRecords,
            UnitOfMeasure = acornSatSeries.UnitOfMeasure,
            DataResolution = acornSatSeries.DataResolution,
        };

        var built = new DataSetBuilder().BuildDataSetFromSeries(body, composedSeries);
        var effectiveRetrievedDate = outcome.Extension.CdoContributed ? outcome.RetrievedDate : null;
        var dataSet = await DataSetEndpoints.BuildResponseDataSet(body, built, effectiveRetrievedDate);

        if (outcome.Extension.CdoContributed)
        {
            var definitions = await DataSetDefinition.GetDataSetDefinitions();
            var cdoDataSet = definitions.Single(x => x.Id == AcornSatDatasetIds.Cdo);
            var cdoMetadata = await new DataSetMetadataBuilder().BuildAsync(cdoDataSet, spec.LocationId);
            dataSet.SourceMetadata = (dataSet.SourceMetadata ?? []).Append(cdoMetadata).ToList();
        }

        return dataSet;
    }

    private static PostDataSetsRequestBody BuildCdoRequest(Guid cdoDataSetDefinitionId, Guid locationId, DataType dataType)
    {
        return new PostDataSetsRequestBody
        {
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = cdoDataSetDefinitionId,
                    LocationId = locationId,
                    DataType = dataType,
                    DataAdjustment = DataAdjustment.Unadjusted,
                },
            ],
            BinningRule = BinGranularities.ByYearAndDay,
            BinAggregationFunction = ContainerAggregationFunctions.Mean,
            CupSize = 1,
            RequiredCupDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredBinDataProportion = 1,
        };
    }

    private static Task<SeriesProvider.Series> ReadSeries(Guid dataSetDefinitionId, Guid locationId, DataType dataType, DataAdjustment dataAdjustment)
    {
        var spec = new SeriesSpecification
        {
            DataSetDefinitionId = dataSetDefinitionId,
            LocationId = locationId,
            DataType = dataType,
            DataAdjustment = dataAdjustment,
        };
        return SeriesProvider.GetSeriesDataRecordsForRequest(SeriesDerivationTypes.ReturnSingleSeries, [spec]);
    }
}
