using System;
using System.Collections.Generic;
using System.Linq;

namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public static class BinAggregator
    {
        public static Bin[] AggregateBins(RawBin[] rawBins, BinAggregationFunctions binAggregationFunction)
        {
            switch (binAggregationFunction)
            {
                case BinAggregationFunctions.Mean:
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
                                    .Select(y => GetAverageOfDataPoints(y))
                                    .ToArray()
                            }
                        )
                        .ToArray();

                    var z =
                        // Second, aggregate the bucket-level values we calculated in step one
                        w
                        .Select(x => new Bin { Identifier = x.RawBin.Identifier, Value = x.BucketAggregates.Average() })
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

        static float GetAverageOfDataPoints(IEnumerable<TemporalDataPoint> dataPoints)
        {
            var aggregate = dataPoints.Where(x => x.Value.HasValue).Average(x => x.Value.Value);

            return aggregate;
        }

    }
}
