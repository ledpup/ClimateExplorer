namespace ClimateExplorer.WebApi;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.WebApi.Metadata;
using Microsoft.AspNetCore.Mvc;
using static ClimateExplorer.Core.Enums;

internal static class DataSetEndpoints
{
    public static async Task<DataSet> PostDataSets(
        PostDataSetsRequestBody body,
        [FromServices] ClimateExplorerApiServices services,
        bool permitSourceUpdate = false,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = "DataSet_v2_" + JsonSerializer.Serialize(body);

        var result = await services.Cache.Get<DataSet>(cacheKey);
        var sourcePreparation = await services.DataSetSourceUpdateCoordinator.PrepareAsync(body, result, permitSourceUpdate, cancellationToken);

        if (result != null && sourcePreparation.Outcome is DataSetSourcePreparationOutcome.UseCached or DataSetSourcePreparationOutcome.RefreshFailed)
        {
            return result;
        }

        var dsb = new DataSetBuilder();

        var series = await dsb.BuildDataSet(body);
        var definitions = await DataSetDefinition.GetDataSetDefinitions();
        var spec = body.SeriesSpecifications![0];

        var geoEntity = await GeographicalEntity.GetGeographicalEntity(spec.LocationId);
        var sourceMetadata = await new DataSetMetadataBuilder().BuildAsync(body, definitions);

        var returnDataSet =
            new DataSet
            {
                GeographicalEntity = geoEntity,
                Resolution = DataResolution.Yearly,
                MeasurementDefinition =
                    new MeasurementDefinitionViewModel
                    {
                        DataAdjustment = spec.DataAdjustment,
                        DataType = spec.DataType,
                        UnitOfMeasure = series.UnitOfMeasure,
                    },
                DataRecords =
                    series.DataPoints
                    .Select(x => new BinnedRecord(x.BinId, x.Value.HasValue ? Math.Round(x.Value.Value, 4) : null)) // 4 decimal places should be enough for anyone
                    .ToList(),
                RawDataRecords =
                    body.IncludeRawDataRecords == true
                    ? series.RawDataRecords
                    : null,
                SourceMetadata = sourceMetadata,
                RetrievedDate = sourcePreparation.Outcome == DataSetSourcePreparationOutcome.RefreshFailed
                    ? null
                    : sourcePreparation.RetrievedDate,
            };

        // If the BinningRule is ByYearAndDay (or ByDayOnly filtered to a specific year) then there is little
        // to gain by caching the data because we haven't done any aggregation. Therefore, return early, before the cache step
        if (body.BinningRule == BinGranularities.ByYearAndDay ||
            (body.BinningRule == BinGranularities.ByDayOnly && body.FilterToYear.HasValue))
        {
            return returnDataSet;
        }

        await services.Cache.Put(cacheKey, returnDataSet);
        return returnDataSet;
    }
}
