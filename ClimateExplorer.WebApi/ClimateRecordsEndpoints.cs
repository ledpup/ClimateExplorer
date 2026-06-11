namespace ClimateExplorer.WebApi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using Microsoft.AspNetCore.Mvc;
using static ClimateExplorer.Core.Enums;

internal static class ClimateRecordsEndpoints
{
    public static async Task<ClimateRecordsResponse> GetClimateRecords(
        [FromServices] ClimateExplorerApiServices services,
        Guid locationId,
        DataType dataType = DataType.TempMax,
        DataAdjustment? dataAdjustment = null,
        bool ascending = false,
        int? take = null,
        int? skip = null,
        int? month = null,
        bool monthly = false,
        int? day = null)
    {
        var dataSetDefinitions = await MetadataEndpoints.GetDataSetDefinitions();
        var locationDsds = dataSetDefinitions.Where(x => x.LocationIds!.Contains(locationId)).ToList();

        MeasurementDefinitionViewModel md = null;
        DataSetDefinitionViewModel matchingDsd = null;

        var suitableResolutions = monthly ? [DataResolution.Daily, DataResolution.Monthly] : new[] { DataResolution.Daily };
        foreach (var resolution in suitableResolutions)
        {
            foreach (var dsd in locationDsds)
            {
                md = dsd.MeasurementDefinitions?.FirstOrDefault(m =>
                    m.DataType == dataType &&
                    m.DataAdjustment == dataAdjustment &&
                    m.DataResolution == resolution);
                if (md != null)
                {
                    matchingDsd = dsd;
                    break;
                }
            }

            if (matchingDsd != null)
            {
                break;
            }
        }

        if (md == null || matchingDsd == null)
        {
            return new ClimateRecordsResponse
            {
                DataAdjustment = dataAdjustment,
                DataType = dataType,
            };
        }

        var responseDataResolution = md.DataResolution == DataResolution.Daily && monthly
            ? DataResolution.Monthly
            : md.DataResolution;

        var fn = dataType == DataType.Precipitation ? ContainerAggregationFunctions.Sum : ContainerAggregationFunctions.Mean;
        var dataSet = await DataSetEndpoints.PostDataSets(
            new PostDataSetsRequestBody
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                BinningRule = md.DataResolution == DataResolution.Daily && !monthly ? BinGranularities.ByYearAndDay : BinGranularities.ByYearAndMonth,
                SeriesTransformation = SeriesTransformations.Identity,
                SeriesSpecifications =
                [
                    new SeriesSpecification
                    {
                        DataSetDefinitionId = matchingDsd.Id,
                        DataType = dataType,
                        LocationId = locationId,
                        DataAdjustment = dataAdjustment,
                    },
                ],
                BinAggregationFunction = fn,
                BucketAggregationFunction = fn,
                CupAggregationFunction = fn,
                RequiredBinDataProportion = 1,
                RequiredBucketDataProportion = 1,
                RequiredCupDataProportion = ClimateExplorerApiConstants.DefaultCupDataProportion,
                CupSize = ClimateExplorerApiConstants.DefaultCupSize,
            },
            services);

        static int YearOf(BinnedRecord r) => r.BinIdentifier switch
        {
            YearAndDayBinIdentifier d => d.Year,
            YearAndMonthBinIdentifier m => m.Year,
            _ => 0,
        };

        var allRecords = dataSet.DataRecords.Where(x => x.Value.HasValue).ToList();
        int? startYear = allRecords.Count > 0 ? allRecords.Min(YearOf) : null;
        int? endYear = allRecords.Count > 0 ? allRecords.Max(YearOf) : null;

        var records = allRecords.AsEnumerable();
        if (month.HasValue)
        {
            records = records.Where(x => x.BinIdentifier switch
            {
                YearAndDayBinIdentifier d => d.Month == month.Value,
                YearAndMonthBinIdentifier m => m.Month == month.Value,
                _ => true,
            });
        }

        if (day.HasValue)
        {
            records = records.Where(x => x.BinIdentifier switch
            {
                YearAndDayBinIdentifier d => d.Day == day.Value,
                _ => true,
            });
        }

        var ordered = ascending
            ? records.OrderBy(x => x.Value)
            : records.OrderByDescending(x => x.Value);

        var totalCount = ordered.Count();

        // Apply pagination if count and/or page is specified
        IEnumerable<BinnedRecord> paginated = ordered;
        if (take.HasValue)
        {
            if (skip.HasValue && skip.Value > 1)
            {
                paginated = paginated.Skip((skip.Value - 1) * take.Value);
            }

            paginated = paginated.Take(take.Value);
        }

        var climateRecords = paginated.Select(record =>
        {
            if (md.DataResolution == DataResolution.Daily && !monthly)
            {
                var dayBin = (YearAndDayBinIdentifier)record.BinIdentifier!;
                return new DataRecord((short)dayBin.Year, (short)dayBin.Month, (short)dayBin.Day, record.Value);
            }
            else
            {
                var monthBin = (YearAndMonthBinIdentifier)record.BinIdentifier!;
                return new DataRecord((short)monthBin.Year, (short)monthBin.Month, record.Value);
            }
        }).ToList();

        var response = new ClimateRecordsResponse
        {
            Records = climateRecords,
            DataAdjustment = dataAdjustment,
            DataResolution = responseDataResolution,
            DataType = dataType,
            UnitOfMeasure = md.UnitOfMeasure,
            StartYear = startYear,
            EndYear = endYear,
            TotalCount = totalCount,
        };
        return response;
    }
}
