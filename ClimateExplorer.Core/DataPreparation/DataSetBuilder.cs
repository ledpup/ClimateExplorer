using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.DataPreparation
{
    public class DataSetBuilder
    {
        public class BuildDataSetResult
        {
            public UnitOfMeasure UnitOfMeasure { get; set; }
            public DataCategory? DataCategory { get; set; }

            public ChartableDataPoint[] DataPoints { get; set; }

            public TemporalDataPoint[] RawDataPoints { get; set; }
        }

        public async Task<BuildDataSetResult> BuildDataSet(PostDataSetsRequestBody request)
        {
            ValidateRequest(request);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Reads raw data (from one or multiple sources) & derive a series from it as per the request
            var series = await SeriesProvider.GetSeriesDataPointsForRequest(request.SeriesDerivationType, request.SeriesSpecifications);

            Console.WriteLine("GetSeriesDataPointsForRequest completed in " + sw.Elapsed);

            // Run the rest of the pipeline (this is a separate method for testability)
            var dataPoints = BuildDataSetFromDataPoints(series.DataPoints, series.DataResolution, request);

            return
                new BuildDataSetResult
                {
                    DataPoints = dataPoints,
                    RawDataPoints = request.IncludeRawDataPoints ? series.DataPoints : null,
                    UnitOfMeasure = series.UnitOfMeasure,
                    DataCategory = series.DataCategory
                };
        }

        public ChartableDataPoint[] BuildDataSetFromDataPoints(TemporalDataPoint[] dataPoints, DataResolution dataResolution, PostDataSetsRequestBody request)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Apply specified transformation (if any) to each data point in the series
            var transformedDataPoints = SeriesTransformer.ApplySeriesTransformation(dataPoints, request.SeriesTransformation);

            Console.WriteLine("ApplySeriesTransformation completed in " + sw.Elapsed);
            sw.Restart();

            // Filter data at series level
            var filteredDataPoints = SeriesFilterer.ApplySeriesFilters(transformedDataPoints, request.FilterToSouthernHemisphereTemperateSeason, request.FilterToTropicalSeason, request.FilterToYear, request.FilterToYearsAfterAndIncluding, request.FilterToYearsBefore);

            Console.WriteLine("ApplySeriesFilters completed in " + sw.Elapsed);
            sw.Restart();

            // When BinningRule is ByYearAndDay, we can drop-out of the data pipeline process here.
            // No aggregation is required because we're just returning the data at the original resolution (i.e., daily)
            if (request.BinningRule == BinGranularities.ByYearAndDay)
            {
                return ConvertDataPointsToChartableDataPoints(filteredDataPoints);
            }

            // Assign to Bins, Buckets and Cups
            var rawBins = Binner.ApplyBinningRules(filteredDataPoints, request.BinningRule, request.CupSize, dataResolution);

            Console.WriteLine("ApplyBinningRules completed in " + sw.Elapsed);
            sw.Restart();

            // Flag bins that have a bucket containing a cup with insufficient data
            var filteredRawBins = 
                BinRejector.ApplyBinRejectionRules(
                    rawBins, 
                    request.RequiredCupDataProportion,
                    request.RequiredBucketDataProportion,
                    request.RequiredBinDataProportion);

            Console.WriteLine("ApplyBinRejectionRules completed in " + sw.Elapsed);
            sw.Restart();

            // Calculate aggregates for each bin
            var aggregatedBins = BinAggregator.AggregateBins(filteredRawBins, request.BinAggregationFunction, request.BucketAggregationFunction, request.CupAggregationFunction, request.SeriesTransformation);

            // Calculate final value based on bin aggregates
            var finalBins = FinalBinValueCalculator.CalculateFinalBinValues(aggregatedBins, request.Anomaly);

            Console.WriteLine("AggregateBins completed in " + sw.Elapsed);
            sw.Restart();

            return
                finalBins
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

        private static ChartableDataPoint[] ConvertDataPointsToChartableDataPoints(TemporalDataPoint[] filteredDataPoints)
        {
            return filteredDataPoints
                .Select(x => (new YearAndDayBinIdentifier(x.Year, x.Month.Value, x.Day.Value), x.Value))
                .Select(
                x =>
                new ChartableDataPoint
                {
                    BinId = x.Item1.Id,
                    Label = x.Item1.Label,
                    Value = x.Item2.GetValueOrDefault(),
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
