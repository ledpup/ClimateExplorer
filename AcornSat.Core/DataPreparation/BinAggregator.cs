using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class BinAggregator
    {
        static float? WeightedMean(IEnumerable<ContainerAggregate> data)
        {
            var containersWithData = data.Where(x => x.Value.HasValue);

            if (!containersWithData.Any()) return null;

            var totalPeriodsCoveredByContainersWithData =
                containersWithData
                .Sum(x => x.NumberOfPeriodsCoveredByAggregate);

            return 
                containersWithData
                .Select(x => x.Value * x.NumberOfPeriodsCoveredByAggregate / totalPeriodsCoveredByContainersWithData)
                .Sum();
        }

        static float? Median(IEnumerable<ContainerAggregate> data)
        {
            var containersWithData = data.Where(x => x.Value.HasValue).ToArray();

            if (!containersWithData.Any()) return null;

            float[] sortedValues = containersWithData.Select(x => x.Value.Value).OrderBy(x => x).ToArray();

            int indexOfMidpoint = sortedValues.Length / 2;

            if (sortedValues.Length % 2 == 1)
            {
                // Odd number of values, so there will be a unique "midpoint"
                return sortedValues[indexOfMidpoint];
            }
            else
            {
                // Even number of values, so return mean of the two "midpoint" values
                return (float)((sortedValues[indexOfMidpoint - 1] + sortedValues[indexOfMidpoint]) / 2);
            }
        }

        static Func<IEnumerable<ContainerAggregate>, float?> GetAggregationFunction(BinAggregationFunctions function)
        {
            switch (function)
            {
                // Weighted mean is mean of value weighted by number of periods covered
                case BinAggregationFunctions.Mean:   return WeightedMean;
                case BinAggregationFunctions.Sum:    return (data) => data.Select(x => x.Value).Sum();
                case BinAggregationFunctions.Min:    return (data) => data.Select(x => x.Value).Min();
                case BinAggregationFunctions.Max:    return (data) => data.Select(x => x.Value).Max();
                case BinAggregationFunctions.Median: return Median;
                default:
                    throw new NotImplementedException($"BinAggregationFunction {function}");
            }
        }

        struct ContainerAggregate
        {
            public float? Value { get; set; }
            public int NumberOfPeriodsCoveredByAggregate { get; set; }
        }

        struct AggregationIntermediate
        {
            public RawBin Bin { get; set; }
            public Bucket Bucket { get; set; }
            public Cup Cup { get; set; }
            public ContainerAggregate Aggregate { get; set; }
        }

        static ContainerAggregate BuildContainerAggregateForCup(Cup cup, Func<IEnumerable<ContainerAggregate>, float?> aggregationFunction)
        {
            return
                new ContainerAggregate
                {
                    NumberOfPeriodsCoveredByAggregate = cup.ExpectedDataPointsInCup,
                    Value = 
                        aggregationFunction(
                            cup.DataPoints
                            .Where(x => x.Value.HasValue)
                            .Select(
                                x => 
                                new ContainerAggregate 
                                { 
                                    Value = x.Value.Value, 
                                    NumberOfPeriodsCoveredByAggregate = 1 
                                }
                            )
                        )
                };
        }

        static Bin[] AggregateBinsByRepeatedlyApplyingAggregationFunctions(
            RawBin[] rawBins, 
            Func<IEnumerable<ContainerAggregate>, float?> binAggregationFunctionImpl,
            Func<IEnumerable<ContainerAggregate>, float?> bucketAggregationFunctionImpl,
            Func<IEnumerable<ContainerAggregate>, float?> cupAggregationFunctionImpl)
        {
            // Strategy: we have an object tree - each bin has a list of buckets, each of which has a list of cups, each of which has a list of data points.
            // We're going to flatten out that tree into a list of AggregationIntermediates, one per cup. Then we're going to repeatedly aggregate, reducing
            // the number of entries in our list of AggregationIntermediates each time, until we have one entry per bin.

            // First, build a list of cups, each with a calculated aggregate based on the data points inside that cup.
            List<AggregationIntermediate> cupLevelIntermediates = new List<AggregationIntermediate>();

            foreach (var bin in rawBins)
            {
                foreach (var bucket in bin.Buckets)
                {
                    foreach (var cup in bucket.Cups)
                    {
                        cupLevelIntermediates.Add(
                            new AggregationIntermediate
                            {
                                Bin = bin,
                                Bucket = bucket,
                                Cup = cup,
                                Aggregate = BuildContainerAggregateForCup(cup, cupAggregationFunctionImpl)
                            }
                        );
                    }
                }
            }

            // Now we have a list of cups. Next, we aggregate up to the bucket level.
            var bucketLevelIntermediates =
                cupLevelIntermediates
                .GroupBy(x => new { x.Bin, x.Bucket })
                .Select(
                    kvp =>
                    new AggregationIntermediate
                    {
                        Bin = kvp.Key.Bin,
                        Bucket = kvp.Key.Bucket,
                        Aggregate = AggregateContainerAggregates(kvp.Select(y => y.Aggregate), bucketAggregationFunctionImpl)
                    }
                )
                .ToArray();

            // Now we have a list of buckets. Next, we aggregate up to the bin level.
            var binLevelIntermediates =
                bucketLevelIntermediates
                .GroupBy(x => new { x.Bin })
                .Select(
                    kvp =>
                    new AggregationIntermediate
                    {
                        Bin = kvp.Key.Bin,
                        Aggregate = AggregateContainerAggregates(kvp.Select(y => y.Aggregate), binAggregationFunctionImpl)
                    }
                )
                .ToArray();

            // That's the data we need. Transform and return it.
            return
                binLevelIntermediates
                .Select(
                    x =>
                    new Bin { Identifier = x.Bin.Identifier, Value = x.Aggregate.Value }
                )
                .ToArray();
        }

        static Bin[] AggregateBinsByApplyingAggregationFunctionOnceForAllDataPoints(
            RawBin[] rawBins, 
            Func<IEnumerable<ContainerAggregate>, float?> aggregationFunction)
        {
            return
                rawBins
                .Select(
                    x =>
                    new Bin
                    {
                        Identifier = x.Identifier,
                        Value = 
                            aggregationFunction(
                                x.Buckets
                                .SelectMany(y => y.Cups)
                                .SelectMany(y => y.DataPoints)
                                .Where(y => y.Value.HasValue)
                                .Select(y => new ContainerAggregate() { Value = y.Value.Value, NumberOfPeriodsCoveredByAggregate = 1 })
                            )
                    }
                )
                .ToArray();
        }

        static ContainerAggregate AggregateContainerAggregates(
            IEnumerable<ContainerAggregate> aggregates,
            Func<IEnumerable<ContainerAggregate>, float?> aggregationFunction)
        {
            return
                new ContainerAggregate()
                {
                    Value = aggregationFunction(aggregates),
                    NumberOfPeriodsCoveredByAggregate = aggregates.Sum(y => y.NumberOfPeriodsCoveredByAggregate)
                };
        }

        public static Bin[] AggregateBins(
            RawBin[] rawBins, 
            BinAggregationFunctions binAggregationFunction,
            BinAggregationFunctions bucketAggregationFunction,
            BinAggregationFunctions cupAggregationFunction)
        {
            if ((bucketAggregationFunction == BinAggregationFunctions.Median) != (cupAggregationFunction == BinAggregationFunctions.Median))
            {
                throw new Exception("If one of bucket and cup aggregation is median, then both must be, because median aggregation is applied once across all data points.");
            }

            if (((bucketAggregationFunction == BinAggregationFunctions.Median) || (cupAggregationFunction == BinAggregationFunctions.Median)) &&
                binAggregationFunction != BinAggregationFunctions.Median)
            {
                throw new Exception("If at least one of bucket and cup aggregation is median, then bin must also be, because median aggregation is applied once across all data points.");
            }

            var binAggregationFunctionImpl = GetAggregationFunction(binAggregationFunction);
            var bucketAggregationFunctionImpl = GetAggregationFunction(bucketAggregationFunction);
            var cupAggregationFunctionImpl = GetAggregationFunction(cupAggregationFunction);

            switch (binAggregationFunction)
            {
                // These aggregation functions give the same result if "repeated" (i.e. calculated for subsets and then recalculated over
                // those subsets). We take advantage of this by calculating the aggregate for each cup (based on the data points each
                // contains), then the aggregate for each bucket (based on the cup-level aggregates calculated in the previous step), then
                // the aggregates for each bin (based on the bucket-level aggregates calculated in the previous step).
                //
                // It's desirable to do this repeated application of aggregation functions where possible because doing so can "smooth out"
                // small gaps in the source data without biasing the result as badly.
                case BinAggregationFunctions.Mean:
                case BinAggregationFunctions.Sum:
                case BinAggregationFunctions.Min:
                case BinAggregationFunctions.Max:
                    return AggregateBinsByRepeatedlyApplyingAggregationFunctions(rawBins, binAggregationFunctionImpl, bucketAggregationFunctionImpl, cupAggregationFunctionImpl);

                // But these aggregation functions do NOT give the same result if repeated in that way. So instead, we calculate the aggregate
                // once, across all available data points.
                case BinAggregationFunctions.Median:
                    return AggregateBinsByApplyingAggregationFunctionOnceForAllDataPoints(rawBins, binAggregationFunctionImpl);

                default:
                    throw new NotImplementedException($"BinAggregationFunction {binAggregationFunction}");
            }
        }
    }
}
