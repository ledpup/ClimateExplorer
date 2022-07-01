using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class BinAggregator
    {
        static float WeightedMean(IEnumerable<ContainerAggregate> data)
        {
            var totalPeriods = data.Sum(x => x.NumberOfPeriodsCoveredByAggregate);

            return data.Select(x => x.Value * x.NumberOfPeriodsCoveredByAggregate / totalPeriods).Sum();
        }

        static Func<IEnumerable<ContainerAggregate>, float> GetAggregationFunction(BinAggregationFunctions function)
        {
            switch (function)
            {
                // Weighted mean is mean of value weighted by number of periods covered
                case BinAggregationFunctions.Mean: return (data) => WeightedMean(data);
                case BinAggregationFunctions.Sum:  return (data) => data.Select(x => x.Value).Sum();
                case BinAggregationFunctions.Min:  return (data) => data.Select(x => x.Value).Min();
                case BinAggregationFunctions.Max:  return (data) => data.Select(x => x.Value).Max();

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
                    // These aggregation functions give the same result if "repeated" (i.e. calculated for subsets and then recalculated over
                    // those subsets)
                    var w =
                        rawBins
                        // First, aggregate the data points in each bucket, keeping track of how many days each bucket represents, so that
                        // we can do weighting later if the aggregation function demands (e.g. mean is implemented as weighted mean by number
                        // of periods)
                        .Select(
                            x =>
                            new
                            {
                                RawBin = x,
                                BucketAggregates =
                                    x.Buckets
                                    .Select(
                                        y =>
                                        new ContainerAggregate()
                                        {
                                            Value =
                                                aggregationFunction(
                                                    GetDataPointsInBucket(y)
                                                    .Where(x => x.Value.HasValue)
                                                    .Select(x => new ContainerAggregate() { Value = x.Value.Value, NumberOfPeriodsCoveredByAggregate = 1 })
                                                ),
                                            NumberOfPeriodsCoveredByAggregate =
                                                y.Cups.Sum(z => z.DaysInCup)
                                        }
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

        struct ContainerAggregate
        {
            public float Value { get; set; }
            public int NumberOfPeriodsCoveredByAggregate { get; set; }
        }
    }
}
