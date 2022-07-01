using AcornSat.Core.InputOutput;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.DataPreparation
{
    public class DataSetBuilder
    {
        public async Task<ChartableDataPoint[]> BuildDataSet(PostDataSetsRequestBody request)
        {
            ValidateRequest(request);

            // Reads raw data (from one or multiple sources) & derive a series from it as per the request
            var dataPoints = await SeriesProvider.GetSeriesDataPointsForRequest(request.SeriesDerivationType, request.SeriesSpecifications);

            return BuildDataSetFromDataPoints(dataPoints, request);
        }

        public ChartableDataPoint[] BuildDataSetFromDataPoints(TemporalDataPoint[] dataPoints, PostDataSetsRequestBody request)
        {
            // Apply specified transformation (if any) to each data point in the series
            var transformedDataPoints = SeriesTransformer.ApplySeriesTransformation(dataPoints, request.SeriesTransformation);

            // Filter data at series level
            var filteredDataPoints = SeriesFilterer.ApplySeriesFilters(transformedDataPoints, request.FilterToTemperateSeason, request.FilterToTropicalSeason, request.FilterToYearsAfterAndIncluding, request.FilterToYearsBefore);

            // Assign to Bins, Buckets and Cups
            var rawBins = Binner.ApplyBinningRules(filteredDataPoints, request.BinningRule, request.SubBinSize);

            // Reject bins that have a bucket containing a cup with insufficient data
            var filteredRawBins = BinRejector.ApplyBinRejectionRules(rawBins, request.SubBinRequiredDataProportion);

            // Calculate aggregates for each bin
            var aggregatedBins = BinAggregator.AggregateBins(filteredRawBins, request.BinAggregationFunction);

            return
                aggregatedBins
                .Select(x => new ChartableDataPoint { Label = x.Identifier.Label, Value = x.Value })
                .ToArray();
        }

        public void ValidateRequest(PostDataSetsRequestBody request)
        {
            if (request.SeriesSpecifications == null) throw new ArgumentNullException(nameof(request.SeriesSpecifications));
        }
    }
}
