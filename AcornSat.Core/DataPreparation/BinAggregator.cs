using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class BinAggregator
    {
        static float WeightedMean(IEnumerable<Tuple<float, int>> data)
        {
            return data.Select(x => x.Item1 * x.Item2 / data.Sum(y => y.Item2)).Sum();
        }

        static Func<IEnumerable<Tuple<float, int>>, float> GetAggregationFunction(BinAggregationFunctions function)
        {
            switch (function)
            {
                // Weighted mean is mean of value (Item1) weighted by number of days (Item2)
                case BinAggregationFunctions.Mean: return (data) => WeightedMean(data);
                case BinAggregationFunctions.Sum:  return (data) => data.Select(x => x.Item1).Sum();
                case BinAggregationFunctions.Min:  return (data) => data.Select(x => x.Item1).Min();
                case BinAggregationFunctions.Max:  return (data) => data.Select(x => x.Item1).Max();

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
                case BinAggregationFunctions.Min:
                case BinAggregationFunctions.Max:
                    var w =
                        rawBins
                        // First, aggregate the data points in each bucket, keeping track of how many days each bucket represents, so that
                        // we can do weighting later if necessary
                        .Select(
                            x =>
                            new
                            {
                                RawBin = x,
                                BucketAggregates =
                                    x.Buckets
                                    .Select(
                                        y =>
                                        new Tuple<float, int>(
                                            // Tuple Item1 is the aggregate value calculated for the bucket
                                            ApplyAggregationFunctionToNonNullDataPoints(
                                                GetDataPointsInBucket(y).Select(x => new Tuple<TemporalDataPoint, int>(x, 1)),
                                                aggregationFunction
                                            ),

                                            // Tuple Item2 is the number of days in the bucket
                                            y.Cups.Sum(z => z.DaysInCup)
                                        )
                                    )
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
            IEnumerable<Tuple<TemporalDataPoint, int>> dataPointsAndNumberOfDaysTheyRepresent, 
            Func<IEnumerable<Tuple<float, int>>, float> aggregationFunction)
        {
            var aggregate = 
                aggregationFunction(
                    dataPointsAndNumberOfDaysTheyRepresent
                    .Where(x => x.Item1.Value.HasValue)
                    .Select(x => new Tuple<float, int>(x.Item1.Value.Value, x.Item2))
                );

            return aggregate;
        }

    }
}
