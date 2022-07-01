using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation.Model
{
    public static class Binner
    {
        public static RawBin[] ApplyBinningRules(TemporalDataPoint[] dataPoints, BinningRules binningRule, int subBinSize)
        {
            var dataPointsByBinId =
                dataPoints
                .ToLookup(x => GetBinIdentifier(x, binningRule));

            switch (binningRule)
            {
                case BinningRules.ByYear:
                case BinningRules.ByYearAndMonth:
                    return
                        dataPointsByBinId
                        .Select(
                            x =>
                            new RawBin()
                            {
                                Identifier = x.Key,
                                Buckets = BuildBucketsForGaplessBin(x, subBinSize)
                            }
                        )
                        .ToArray();

                default:
                    return
                        dataPointsByBinId
                        .Select(
                            x =>
                            new RawBin()
                            {
                                Identifier = x.Key,
                                Buckets =
                                    // For now, just put everything in a single cup in a single bucket
                                    // TODO: the right thing
                                    new Bucket[]
                                    {
                                        new Bucket
                                        {
                                            Cups =
                                                new Cup[]
                                                {
                                                    new Cup
                                                    {
                                                        DataPoints = x.ToArray()
                                                    }
                                                }
                                        }
                                    }
                            }
                        )
                        .ToArray();
            }
        }

        static Bucket[] BuildBucketsForGaplessBin(IGrouping<BinIdentifier, TemporalDataPoint> bin, int subBinSize)
        {
            // For gapless bins, buckets and cups are the same size
            var binIdentifier = bin.Key as BinIdentifierForGaplessBin;

            if (binIdentifier == null)
            {
                throw new Exception($"Cannot treat BinIdentifier of type {bin.Key.GetType().Name} as a {nameof(BinIdentifierForGaplessBin)}");
            }

            List<Bucket> buckets = new List<Bucket>();

            var d = binIdentifier.FirstDayInBin;

            List<TemporalDataPoint> bucketPoints = new List<TemporalDataPoint>();

            var points = bin.ToArray();
            var i = 0;
            int bucketCounter = 0;

            var firstDayInBucket = d;

            while (d <= binIdentifier.LastDayInBin && i < points.Length)
            {
                if (points[i].Year == d.Year && points[i].Month == d.Month && points[i].Day == d.Day)
                {
                    bucketPoints.Add(points[i]);
                    i++;
                }

                bucketCounter++;
                if (bucketCounter == subBinSize)
                {
                    buckets.Add(
                        new Bucket
                        {
                            Cups =
                                new Cup[]
                                {
                                    new Cup
                                    {
                                        DataPoints = bucketPoints.ToArray(),
                                        FirstDayInCup = firstDayInBucket,
                                        LastDayInCup = d
                                    }
                                }
                        }
                    );

                    bucketPoints = new List<TemporalDataPoint>();
                    bucketCounter = 0;
                    firstDayInBucket = d.AddDays(1);
                }

                d = d.AddDays(1);
            }

            // If we have leftovers, add them to the last bucket
            if (bucketCounter > 0)
            {
                var lastCup = buckets.Last().Cups.Last();
                lastCup.DataPoints = lastCup.DataPoints.Concat(bucketPoints).ToArray();
                lastCup.LastDayInCup = d.AddDays(-1);
            }

            return buckets.ToArray();
        }

        static BinIdentifier GetBinIdentifier(TemporalDataPoint dp, BinningRules binningRule)
        {
            // TODO: Notice that (for example) for a year with 365 data points, we will allocate 365 otherwise identical year bin identifiers.
            // We could cache them centrally and re-use to reduce allocations.
            switch (binningRule)
            {
                case BinningRules.ByYear: return new YearBinIdentifier(dp.Year);
                case BinningRules.ByYearAndMonth: return new YearAndMonthBinIdentifier(dp.Year, dp.Month.Value);
                case BinningRules.ByMonthOnly: return new MonthOnlyBinIdentifier(dp.Month.Value);
                case BinningRules.ByTemperateSeasonOnly: return new TemperateSeasonOnlyBinIdentifier(DateHelpers.GetTemperateSeasonForMonth(dp.Month.Value));
                case BinningRules.ByTropicalSeasonOnly: return new TropicalSeasonOnlyBinIdentifier(DateHelpers.GetTropicalSeasonForMonth(dp.Month.Value));
                default: throw new NotImplementedException($"BinningRule {binningRule}");
            }
        }
    }
}
