using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class BinAggregator
    {
        static Func<IEnumerable<float>, float> GetAggregationFunction(BinAggregationFunctions function)
        {
            switch (function)
            {
                case BinAggregationFunctions.Mean: return (x) => x.Average();
                case BinAggregationFunctions.Sum: return (x) => x.Sum();

                default:
                    throw new NotImplementedException($"BinAggregationFunction {function}");
            }
        }

        public static Bin[] AggregateBins(RawBin[] rawBins, BinAggregationFunctions binAggregationFunction)
        {
            var aggregationFunction = GetAggregationFunction(binAggregationFunction);

            switch (binAggregationFunction)
            {
                case BinAggregationFunctions.Mean:
                case BinAggregationFunctions.Sum:
                    var w =
                        rawBins
                        // First, aggregate the data points in each bucket
                        .Select(
                            x =>
                            new
                            {
                                RawBin = x,
                                BucketAggregates =
                                    x.Buckets
                                    .Select(y => GetDataPointsInBucket(y))
                                    .Select(y => ApplyAggregationFunctionToNonNullDataPoints(y, aggregationFunction))
                                    .ToArray()
                            }
                        )
                        .ToArray();

                    var z =
                        // Second, aggregate the bucket-level values we calculated in step one
                        w
                        .Select(x => new Bin { Identifier = x.RawBin.Identifier, Value = aggregationFunction(x.BucketAggregates) })
                        .ToArray();

                    return z;

                default:
                    throw new NotImplementedException($"BinAgregationFunction {binAggregationFunction}");
            }
        }

        static IEnumerable<TemporalDataPoint> GetDataPointsInBucket(Bucket bucket)
        {
            var dataPoints = bucket.Cups.SelectMany(x => x.DataPoints);

            return dataPoints;
        }

        static float ApplyAggregationFunctionToNonNullDataPoints(
            IEnumerable<TemporalDataPoint> dataPoints, 
            Func<IEnumerable<float>, float> aggregationFunction)
        {
            var aggregate = 
                aggregationFunction(
                    dataPoints
                    .Where(x => x.Value.HasValue)
                    .Select(x => x.Value.Value)
                );

            return aggregate;
        }

    }
}
