using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation.Model
{
    public static class Binner
    {
        public static RawBin[] ApplyBinningRules(TemporalDataPoint[] dataPoints, BinningRules binningRule, int cupSize)
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
                        Buckets = BuildBucketsForBin(binningRule, x, cupSize)
                    }
                )
                .ToArray();
        }

        static Bucket[] BuildBucketsForBin(
            BinningRules binningRule, 
            IGrouping<BinIdentifier, TemporalDataPoint> bin, 
            int cupSize)
        {
            switch (binningRule)
            {
                case BinningRules.ByYear:
                case BinningRules.ByYearAndMonth:
                    return BuildBucketsForGaplessBin(bin, cupSize);

                case BinningRules.ByMonthOnly:
                    return BuildBucketsForMonth(bin, cupSize);

                case BinningRules.BySouthernHemisphereTemperateSeasonOnly:
                    return BuildBucketsForSouthernHemisphereTemperateSeason(bin, cupSize);

                default:
                    throw new NotImplementedException($"BinningRule {binningRule}");
            }
        }

        static Bucket[] BuildBucketsForMonth(
            IGrouping<BinIdentifier, TemporalDataPoint> bin,
            int cupSize)
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
                                cupSize)
                            .ToArray()
                    }
                );
            }
            
            return buckets.ToArray();
        }

        static Bucket[] BuildBucketsForSouthernHemisphereTemperateSeason(
            IGrouping<BinIdentifier, TemporalDataPoint> bin,
            int cupSize)
        {
            // Bin is temperate season
            // Bucket is year + temperate season
            // Cup is a cupSize segment of bucket
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
                                cupSize)
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
            int cupSize)
        {
            // For gapless bins, buckets and cups are the same size.
            var binIdentifier = bin.Key as BinIdentifierForGaplessBin;

            if (binIdentifier == null)
            {
                throw new Exception($"Cannot treat BinIdentifier of type {bin.Key.GetType().Name} as a {nameof(BinIdentifierForGaplessBin)}");
            }

            var cupSegments =
                DateHelpers.DivideDateSpanIntoSegmentsWithIncompleteFinalSegmentAddedToFinalSegment(
                    binIdentifier.FirstDayInBin,
                    binIdentifier.LastDayInBin,
                    cupSize
                );

            var points = bin.ToArray();

            var cups =
                cupSegments
                .Select(
                    x =>
                    new Cup
                    {
                        FirstDayInCup = x.Start,
                        LastDayInCup = x.End,
                        DataPoints = points.Where(y => DataPointFallsInInclusiveRange(y, x.Start, x.End)).ToArray()
                    }
                );

            var buckets =
                cups
                .Select(
                    x =>
                    new Bucket
                    {
                        FirstDayInBucket = x.FirstDayInCup,
                        LastDayInBucket = x.LastDayInCup,
                        Cups = new Cup[] { x }
                    }
                )
                .ToArray();

            return buckets;
        }

        static bool DataPointFallsInInclusiveRange(TemporalDataPoint dp, DateOnly start, DateOnly end)
        {
            DateOnly d = new DateOnly(dp.Year, dp.Month ?? 1, dp.Day ?? 1);

            return d >= start && d <= end;
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
