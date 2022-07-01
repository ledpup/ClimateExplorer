using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation.Model
{
    public static class Binner
    {
        public static RawBin[] ApplyBinningRules(TemporalDataPoint[] dataPoints, BinningRules binningRule, int CupSize)
        {
            var dataPointsByBinId =
                dataPoints
                .ToLookup(x => GetBinIdentifier(x, binningRule));

            return
                dataPointsByBinId
                .Select(
                    x =>
                    new RawBin()
                    {
                        Identifier = x.Key,
                        Buckets = BuildBucketsForBin(binningRule, x, CupSize)
                    }
                )
                .ToArray();
        }

        static Bucket[] BuildBucketsForBin(
            BinningRules binningRule, 
            IGrouping<BinIdentifier, TemporalDataPoint> bin, 
            int CupSize)
        {
            switch (binningRule)
            {
                case BinningRules.ByYear:
                case BinningRules.ByYearAndMonth:
                    return BuildBucketsForGaplessBin(bin, CupSize);

                case BinningRules.ByMonthOnly:
                    return BuildBucketsForMonth(bin, CupSize);

                case BinningRules.BySouthernHemisphereTemperateSeasonOnly:
                    return BuildBucketsForSouthernHemisphereTemperateSeason(bin, CupSize);

                default:
                    throw new NotImplementedException($"BinningRule {binningRule}");
            }
        }

        static Bucket[] BuildBucketsForMonth(
            IGrouping<BinIdentifier, TemporalDataPoint> bin,
            int CupSize)
        {
            // Bin is month
            // Bucket is year + month
            // Cup is a CupSize segment of bucket
            var binIdentifier = bin.Key as MonthOnlyBinIdentifier;

            List<Bucket> buckets = new List<Bucket>();

            var dataPointsByMonth = bin.GroupBy(x => new { x.Year, x.Month });

            foreach (var group in dataPointsByMonth)
            {
                buckets.Add(
                    new Bucket
                    {
                        Cups = 
                            BuildCupsForDataPointRange(
                                new DateOnly(group.Key.Year, group.Key.Month.Value, 1),
                                DateHelpers.GetLastDayInMonth(group.Key.Year, group.Key.Month.Value),
                                group.ToArray(),
                                CupSize)
                            .ToArray()
                    }
                );
            }
            
            return buckets.ToArray();
        }

        static Bucket[] BuildBucketsForSouthernHemisphereTemperateSeason(
            IGrouping<BinIdentifier, TemporalDataPoint> bin,
            int CupSize)
        {
            // Bin is temperate season
            // Bucket is year + temperate season
            // Cup is a CupSize segment of bucket
            var binIdentifier = bin.Key as SouthernHemisphereTemperateSeasonOnlyBinIdentifier;

            List<Bucket> buckets = new List<Bucket>();

            var dataPointsBySeasonOccurrence = bin.GroupBy(x => DateHelpers.GetSouthernHemisphereTemperateSeasonAndYear(x.Year, x.Month.Value));

            foreach (var seasonOccurrence in dataPointsBySeasonOccurrence)
            {
                buckets.Add(
                    new Bucket
                    {
                        Cups =
                            BuildCupsForDataPointRange(
                                DateHelpers.GetFirstDayInTemperateSeasonOccurrence(seasonOccurrence.Key),
                                DateHelpers.GetLastDayInTemperateSeasonOccurrence(seasonOccurrence.Key),
                                seasonOccurrence.ToArray(),
                                CupSize)
                            .ToArray()
                    }
                );
            }

            return buckets.ToArray();
        }

        static IEnumerable<Cup> BuildCupsForDataPointRange(
            DateOnly firstDay, 
            DateOnly lastDay, 
            TemporalDataPoint[] points,
            int cupSize)
        {
            DateOnly d = firstDay;

            int i = 0;
            int cupCounter = 0;
            DateOnly firstDayInCup = firstDay;

            List<TemporalDataPoint> cupPoints = new List<TemporalDataPoint>();

            List<Cup> cups = new List<Cup>();

            while (d <= lastDay && i < points.Length)
            {
                if (points[i].Year == d.Year && points[i].Month == d.Month && points[i].Day == d.Day)
                {
                    cupPoints.Add(points[i]);
                    i++;
                }

                cupCounter++;
                if (cupCounter == cupSize || i == points.Length)
                {
                    cups.Add(
                        new Cup
                        {
                            DataPoints = cupPoints.ToArray(),
                            FirstDayInCup = firstDayInCup,
                            LastDayInCup = d
                        }
                    );

                    cupPoints = new List<TemporalDataPoint>();
                    cupCounter = 0;
                    firstDayInCup = d.AddDays(1);
                }

                d = d.AddDays(1);
            }

            // If we have leftovers, add them to the last cup
            if (cupCounter > 0)
            {
                var lastCup = cups.Last();
                lastCup.DataPoints = lastCup.DataPoints.Concat(cupPoints).ToArray();
                lastCup.LastDayInCup = d.AddDays(-1);
            }

            return cups;
        }

        static Bucket[] BuildBucketsForGaplessBin(
            IGrouping<BinIdentifier, TemporalDataPoint> bin, 
            int CupSize)
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
                if (bucketCounter == CupSize || i == points.Length)
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
                case BinningRules.BySouthernHemisphereTemperateSeasonOnly: return new SouthernHemisphereTemperateSeasonOnlyBinIdentifier(DateHelpers.GetSouthernHemisphereTemperateSeasonForMonth(dp.Month.Value));
                case BinningRules.ByTropicalSeasonOnly: return new TropicalSeasonOnlyBinIdentifier(DateHelpers.GetTropicalSeasonForMonth(dp.Month.Value));
                default: throw new NotImplementedException($"BinningRule {binningRule}");
            }
        }
    }
}
