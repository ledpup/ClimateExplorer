using ClimateExplorer.Core.DataPreparation.Model;

namespace ClimateExplorer.Core.DataPreparation;

public static class BinAggregator
{
    static double? WeightedMean(IEnumerable<ContainerAggregate> data)
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

    static double? Median(IEnumerable<ContainerAggregate> data)
    {
        var containersWithData = data.Where(x => x.Value.HasValue).ToArray();

        if (!containersWithData.Any()) return null;

        double[] sortedValues = containersWithData.Select(x => x.Value!.Value).OrderBy(x => x).ToArray();

        int indexOfMidpoint = sortedValues.Length / 2;

        if (sortedValues.Length % 2 == 1)
        {
            // Odd number of values, so there will be a unique "midpoint"
            return sortedValues[indexOfMidpoint];
        }
        else
        {
            // Even number of values, so return mean of the two "midpoint" values
            return (double)((sortedValues[indexOfMidpoint - 1] + sortedValues[indexOfMidpoint]) / 2);
        }
    }

    static Func<IEnumerable<ContainerAggregate>, double?> GetAggregationFunction(ContainerAggregationFunctions function, SeriesTransformations seriesTransformation)
    {
        switch (function)
        {
            // Weighted mean is mean of value weighted by number of periods covered
            case ContainerAggregationFunctions.Mean:        return WeightedMean;
            case ContainerAggregationFunctions.Sum:         return (data) => data.Select(x => x.Value).Sum();
            case ContainerAggregationFunctions.Min:         return (data) => data
                                                                            .Where(x => seriesTransformation != SeriesTransformations.DayOfYearIfFrost || x.Value != 0)
                                                                            .Select(x => x.Value)
                                                                            .Min();
            case ContainerAggregationFunctions.Max:         return (data) => data.Select(x => x.Value).Max();
            case ContainerAggregationFunctions.Median:      return Median;
            default:
                throw new NotImplementedException($"BinAggregationFunction {function}");
        }
    }

    struct ContainerAggregate
    {
        public double? Value { get; set; }
        public int NumberOfPeriodsCoveredByAggregate { get; set; }
    }

    struct AggregationIntermediate
    {
        public RawBinWithDataAdequacyFlag Bin { get; set; }
        public Bucket Bucket { get; set; }
        public Cup Cup { get; set; }
        public ContainerAggregate Aggregate { get; set; }
    }

    static ContainerAggregate BuildContainerAggregateForCup(Cup cup, Func<IEnumerable<ContainerAggregate>, double?> aggregationFunction)
    {
        return
            new ContainerAggregate
            {
                NumberOfPeriodsCoveredByAggregate = cup.ExpectedDataPointsInCup,
                Value = 
                    aggregationFunction(
                        cup.DataPoints!
                        .Where(x => x.Value.HasValue)
                        .Select(
                            x => 
                            new ContainerAggregate 
                            { 
                                Value = x.Value!.Value, 
                                NumberOfPeriodsCoveredByAggregate = 1 
                            }
                        )
                    )
            };
    }

    static Bin[] AggregateBinsByRepeatedlyApplyingAggregationFunctions(
        RawBinWithDataAdequacyFlag[] rawBins, 
        Func<IEnumerable<ContainerAggregate>, double?> binAggregationFunctionImpl,
        Func<IEnumerable<ContainerAggregate>, double?> bucketAggregationFunctionImpl,
        Func<IEnumerable<ContainerAggregate>, double?> cupAggregationFunctionImpl)
    {
        // Strategy: we have an object tree - each bin has a list of buckets, each of which has a list of cups, each of which has a list of data points.
        // We're going to flatten out that tree into a list of AggregationIntermediates, one per cup. Then we're going to repeatedly aggregate, reducing
        // the number of entries in our list of AggregationIntermediates each time, until we have one entry per bin.

        // First, build a list of cups, each with a calculated aggregate based on the data points inside that cup.
        List<AggregationIntermediate> cupLevelIntermediates = [];

        foreach (var bin in rawBins)
        {
            foreach (var bucket in bin.Buckets!)
            {
                foreach (var cup in bucket.Cups!)
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
                new Bin { Identifier = x.Bin.Identifier, Value = x.Bin.MeetsDataRequirements ? x.Aggregate.Value : null }
            )
            .ToArray();
    }

    static Bin[] AggregateBinsByApplyingAggregationFunctionOnceForAllDataPoints(
        RawBinWithDataAdequacyFlag[] rawBins, 
        Func<IEnumerable<ContainerAggregate>, double?> aggregationFunction)
    {
        return
            rawBins
            .Select(
                x =>
                new Bin
                {
                    Identifier = x.Identifier,
                    Value = 
                        x.MeetsDataRequirements
                        ? aggregationFunction(
                            x.Buckets!
                            .SelectMany(y => y.Cups!)
                            .SelectMany(y => y.DataPoints!)
                            .Where(y => y.Value.HasValue)
                            .Select(y => new ContainerAggregate() { Value = y.Value!.Value, NumberOfPeriodsCoveredByAggregate = 1 })
                        )
                        : null
                }
            )
            .ToArray();
    }

    static ContainerAggregate AggregateContainerAggregates(
        IEnumerable<ContainerAggregate> aggregates,
        Func<IEnumerable<ContainerAggregate>, double?> aggregationFunction)
    {
        return
            new ContainerAggregate()
            {
                Value = aggregationFunction(aggregates),
                NumberOfPeriodsCoveredByAggregate = aggregates.Sum(y => y.NumberOfPeriodsCoveredByAggregate)
            };
    }

    public static Bin[] AggregateBins(
        RawBinWithDataAdequacyFlag[] rawBins, 
        ContainerAggregationFunctions binAggregationFunction,
        ContainerAggregationFunctions bucketAggregationFunction,
        ContainerAggregationFunctions cupAggregationFunction,
        SeriesTransformations seriesTransformation)
    {
        if ((bucketAggregationFunction == ContainerAggregationFunctions.Median) != (cupAggregationFunction == ContainerAggregationFunctions.Median))
        {
            throw new Exception("If one of bucket and cup aggregation is median, then both must be, because median aggregation is applied once across all data points.");
        }

        if (((bucketAggregationFunction == ContainerAggregationFunctions.Median) || (cupAggregationFunction == ContainerAggregationFunctions.Median)) &&
            binAggregationFunction != ContainerAggregationFunctions.Median)
        {
            throw new Exception("If at least one of bucket and cup aggregation is median, then bin must also be, because median aggregation is applied once across all data points.");
        }

        var binAggregationFunctionImpl = GetAggregationFunction(binAggregationFunction, seriesTransformation);
        var bucketAggregationFunctionImpl = GetAggregationFunction(bucketAggregationFunction, seriesTransformation);
        var cupAggregationFunctionImpl = GetAggregationFunction(cupAggregationFunction, seriesTransformation);

        switch (binAggregationFunction)
        {
            // These aggregation functions give the same result if "repeated" (i.e. calculated for subsets and then recalculated over
            // those subsets). We take advantage of this by calculating the aggregate for each cup (based on the data points each
            // contains), then the aggregate for each bucket (based on the cup-level aggregates calculated in the previous step), then
            // the aggregates for each bin (based on the bucket-level aggregates calculated in the previous step).
            //
            // It's desirable to do this repeated application of aggregation functions where possible because doing so can "smooth out"
            // small gaps in the source data without biasing the result as badly.
            case ContainerAggregationFunctions.Mean:
            case ContainerAggregationFunctions.Sum:
            case ContainerAggregationFunctions.Min:
            case ContainerAggregationFunctions.Max:
                return AggregateBinsByRepeatedlyApplyingAggregationFunctions(rawBins, binAggregationFunctionImpl, bucketAggregationFunctionImpl, cupAggregationFunctionImpl);

            // But these aggregation functions do NOT give the same result if repeated in that way. So instead, we calculate the aggregate
            // once, across all available data points.
            case ContainerAggregationFunctions.Median:
                return AggregateBinsByApplyingAggregationFunctionOnceForAllDataPoints(rawBins, binAggregationFunctionImpl);

            default:
                throw new NotImplementedException($"BinAggregationFunction {binAggregationFunction}");
        }
    }
}
