using AcornSat.Core.InputOutput;
using AcornSat.Core.ViewModel;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace ClimateExplorer.Core.DataPreparation
{
    public class DataSetBuilder
    {
        public class BuildDataSetResult
        {
            public UnitOfMeasure UnitOfMeasure { get; set; }
            public DataCategory? DataCategory { get; set; }

            public ChartableDataPoint[] DataPoints { get; set; }
        }

        public async Task<BuildDataSetResult> BuildDataSet(PostDataSetsRequestBody request)
        {
            ValidateRequest(request);

            // Reads raw data (from one or multiple sources) & derive a series from it as per the request
            var series = await SeriesProvider.GetSeriesDataPointsForRequest(request.SeriesDerivationType, request.SeriesSpecifications);

            // Run the rest of the pipeline (this is a separate method for testability)
            var dataPoints = BuildDataSetFromDataPoints(series.DataPoints, request);

            return
                new BuildDataSetResult
                {
                    DataPoints = dataPoints,
                    UnitOfMeasure = series.UnitOfMeasure,
                    DataCategory = series.DataCategory
                };
        }

        public ChartableDataPoint[] BuildDataSetFromDataPoints(TemporalDataPoint[] dataPoints, PostDataSetsRequestBody request)
        {
            // Apply specified transformation (if any) to each data point in the series
            var transformedDataPoints = SeriesTransformer.ApplySeriesTransformation(dataPoints, request.SeriesTransformation);

            // Filter data at series level
            var filteredDataPoints = SeriesFilterer.ApplySeriesFilters(transformedDataPoints, request.FilterToSouthernHemisphereTemperateSeason, request.FilterToTropicalSeason, request.FilterToYearsAfterAndIncluding, request.FilterToYearsBefore);

            // Assign to Bins, Buckets and Cups
            var rawBins = Binner.ApplyBinningRules(filteredDataPoints, request.BinningRule, request.CupSize);

            // Reject bins that have a bucket containing a cup with insufficient data
            var filteredRawBins = 
                BinRejector.ApplyBinRejectionRules(
                    rawBins, 
                    request.RequiredCupDataProportion,
                    request.RequiredBucketDataProportion,
                    request.RequiredBinDataProportion);

            // Calculate aggregates for each bin
            var aggregatedBins = BinAggregator.AggregateBins(filteredRawBins, request.BinAggregationFunction);

            return
                aggregatedBins
                .Select(
                    x => 
                    new ChartableDataPoint 
                    {
                        BinId = x.Identifier.Id,
                        Label = x.Identifier.Label, 
                        Value = x.Value
                    }
                )
                .ToArray();
        }

        public void ValidateRequest(PostDataSetsRequestBody request)
        {
            if (request.SeriesSpecifications == null) throw new ArgumentNullException(nameof(request.SeriesSpecifications));
        }
    }
}
