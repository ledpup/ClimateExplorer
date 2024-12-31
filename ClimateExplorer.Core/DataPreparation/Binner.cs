namespace ClimateExplorer.Core.DataPreparation.Model;

using ClimateExplorer.Core.Model;
using System.Globalization;
using static ClimateExplorer.Core.Enums;

public static class Binner
{
    public static RawBin[] ApplyBinningRules(DataRecord[] dataRecords, BinGranularities binningRule, int cupSize, DataResolution dataResolution)
    {
        var dataRecordsByBinId =
            dataRecords
            .ToLookup(x => GetBinIdentifier(x, binningRule));

        return
            dataRecordsByBinId
            .Select(
                x =>
                new RawBin()
                {
                    Identifier = x.Key,
                    Buckets = BuildBucketsForBin(binningRule, x, cupSize, dataResolution),
                })
            .ToArray();
    }

    // This presumes that weeks start with Monday.
    // Week 1 is the 1st week of the year with a Thursday in it.
    public static int GetIso8601WeekOfYear(DateTime time)
    {
        // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll
        // be the same week# as whatever Thursday, Friday or Saturday are,
        // and we always get those right
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            time = time.AddDays(3);
        }

        // Return the week of our adjusted day
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static Bucket[] BuildBucketsForBin(
        BinGranularities binningRule,
        IGrouping<BinIdentifier, DataRecord> bin,
        int cupSize,
        DataResolution dataResolution)
    {
        switch (binningRule)
        {
            case BinGranularities.ByYear:
            case BinGranularities.ByYearAndMonth:
            case BinGranularities.ByYearAndWeek:
                return BuildBucketsForGaplessBin(bin, cupSize, dataResolution);

            case BinGranularities.ByMonthOnly:
                return BuildBucketsForMonth(bin, cupSize, dataResolution);

            case BinGranularities.BySouthernHemisphereTemperateSeasonOnly:
                return BuildBucketsForSouthernHemisphereTemperateSeason(bin, cupSize, dataResolution);

            case BinGranularities.BySouthernHemisphereTropicalSeasonOnly:
                return BuildBucketsForSouthernHemisphereTropicalSeason(bin, cupSize, dataResolution);

            default:
                throw new NotImplementedException($"BinningRule {binningRule}");
        }
    }

    private static Bucket[] BuildBucketsForMonth(
        IGrouping<BinIdentifier, DataRecord> bin,
        int cupSize,
        DataResolution dataResolution)
    {
        // Bin is month
        // Bucket is year + month
        // Cup is a CupSize segment of bucket
        var binIdentifier = bin.Key as MonthOnlyBinIdentifier;

        List<Bucket> buckets = [];

        var dataRecordsByMonth = bin.GroupBy(x => new { x.Year, x.Month });

        foreach (var group in dataRecordsByMonth)
        {
            var firstDayInMonth = new DateOnly(group.Key.Year, group.Key.Month!.Value, 1);
            var lastDayInMonth = DateHelpers.GetLastDayInMonth(group.Key.Year, group.Key.Month.Value);

            switch (dataResolution)
            {
                case DataResolution.Daily:
                    buckets.Add(
                        new Bucket
                        {
                            Cups =
                                BuildCupsForDataRecordRange(
                                    firstDayInMonth,
                                    lastDayInMonth,
                                    group.ToArray(),
                                    cupSize)
                                .ToArray(),
                        });
                    break;
                case DataResolution.Monthly:
                    buckets.Add(
                        new Bucket
                        {
                            Cups =
                                [
                                    new Cup
                                    {
                                        FirstDayInCup = firstDayInMonth,
                                        LastDayInCup = lastDayInMonth,
                                        DataRecords = group.ToArray(),
                                        ExpectedDataRecordsInCup = 1,
                                    },
                                ],
                        });
                    break;
                default:
                    throw new Exception($"Cannot convert underlying data at resolution {dataResolution} into bins at monthly level");
            }
        }

        return buckets.ToArray();
    }

    private static Bucket[] BuildBucketsForSouthernHemisphereTemperateSeason(
        IGrouping<BinIdentifier, DataRecord> bin,
        int cupSize,
        DataResolution dataResolution)
    {
        // Bin is temperate season
        // Bucket is year + temperate season
        // Cup is a cupSize segment of bucket
        var binIdentifier = bin.Key as SouthernHemisphereTemperateSeasonOnlyBinIdentifier;

        List<Bucket> buckets = [];

        var dataRecordsBySeasonOccurrence = bin.GroupBy(x => DateHelpers.GetSouthernHemisphereTemperateSeasonAndYear(x.Year, x.Month!.Value));

        foreach (var seasonOccurrence in dataRecordsBySeasonOccurrence)
        {
            buckets.Add(
                new Bucket
                {
                    Cups =
                        BuildCupsForDataRecordRange(
                            DateHelpers.GetFirstDayInTemperateSeasonOccurrence(seasonOccurrence.Key),
                            DateHelpers.GetLastDayInTemperateSeasonOccurrence(seasonOccurrence.Key),
                            seasonOccurrence.ToArray(),
                            cupSize)
                        .ToArray(),
                });
        }

        return buckets.ToArray();
    }

    private static Bucket[] BuildBucketsForSouthernHemisphereTropicalSeason(
        IGrouping<BinIdentifier, DataRecord> bin,
        int cupSize,
        DataResolution dataResolution)
    {
        // Bin is tropical season
        // Bucket is year + temperate season
        // Cup is a cupSize segment of bucket
        var binIdentifier = bin.Key as SouthernHemisphereTropicalSeasonOnlyBinIdentifier;

        List<Bucket> buckets = [];

        var dataRecordsBySeasonOccurrence = bin.GroupBy(x => DateHelpers.GetSouthernHemisphereTropicalSeasonAndYear(x.Year, x.Month!.Value));

        foreach (var seasonOccurrence in dataRecordsBySeasonOccurrence)
        {
            buckets.Add(
                new Bucket
                {
                    Cups =
                        BuildCupsForDataRecordRange(
                            DateHelpers.GetFirstDayInTropicalSeasonOccurrence(seasonOccurrence.Key),
                            DateHelpers.GetLastDayInTropicalSeasonOccurrence(seasonOccurrence.Key),
                            seasonOccurrence.ToArray(),
                            cupSize)
                        .ToArray(),
                });
        }

        return buckets.ToArray();
    }

    private static IEnumerable<Cup> BuildMonthlyCupsForMonthlyData(
        DateOnly firstDay,
        DateOnly lastDay,
        DataRecord[] records)
    {
        var cupSegments =
            DateHelpers.DivideDateSpanIntoMonthSegments(firstDay, lastDay);

        var cups =
            cupSegments
            .Select(
                x =>
                new Cup
                {
                    FirstDayInCup = x.Start,
                    LastDayInCup = x.End,
                    DataRecords = records.Where(y => DataRecordFallsInInclusiveRange(y, x.Start, x.End)).ToArray(),
                    ExpectedDataRecordsInCup = 1,
                });

        return cups;
    }

    private static IEnumerable<Cup> BuildYearlyCupsForYearlyData(
    DateOnly firstDay,
    DateOnly lastDay,
    DataRecord[] records)
    {
        var cups =
            records
            .Select(
                x =>
                new Cup
                {
                    FirstDayInCup = firstDay,
                    LastDayInCup = lastDay,
                    DataRecords = [x],
                    ExpectedDataRecordsInCup = 1,
                });

        return cups;
    }

    private static IEnumerable<Cup> BuildCupsForDataRecordRange(
        DateOnly firstDay,
        DateOnly lastDay,
        DataRecord[] records,
        int cupSize)
    {
        var cupSegments =
            DateHelpers.DivideDateSpanIntoSegmentsWithIncompleteFinalSegmentAddedToFinalSegment(
                firstDay,
                lastDay,
                cupSize);

        var cups =
            cupSegments
            .Select(
                x =>
                new Cup
                {
                    FirstDayInCup = x.Start,
                    LastDayInCup = x.End,
                    DataRecords = records.Where(y => DataRecordFallsInInclusiveRange(y, x.Start, x.End)).ToArray(),
                    ExpectedDataRecordsInCup = DateHelpers.CountDaysInRange(x.Start, x.End),
                });

        return cups;
    }

    private static Bucket[] BuildBucketsForGaplessBin(
        IGrouping<BinIdentifier, DataRecord> bin,
        int cupSize,
        DataResolution dataResolution)
    {
        var binIdentifier = bin.Key as BinIdentifierForGaplessBin;

        if (binIdentifier == null)
        {
            throw new Exception($"Cannot treat BinIdentifier of type {bin.Key.GetType().Name} as a {nameof(BinIdentifierForGaplessBin)}");
        }

        // For gapless bins, buckets and cups are the same size - we just build cups covering the bin, and assign each cup to one bucket.
        IEnumerable<Cup> cups;

        switch (dataResolution)
        {
            case DataResolution.Daily:
                // If the underlying data is daily resolution, we honour the cup size
                cups =
                    BuildCupsForDataRecordRange(
                        binIdentifier.FirstDayInBin,
                        binIdentifier.LastDayInBin,
                        bin.ToArray(),
                        cupSize);

                break;

            case DataResolution.Monthly:
                // If the underlying data is monthly resolution, we create one cup per month in the bin
                cups = BuildMonthlyCupsForMonthlyData(binIdentifier.FirstDayInBin, binIdentifier.LastDayInBin, bin.ToArray());
                break;

            case DataResolution.Yearly:
                cups = BuildYearlyCupsForYearlyData(new DateOnly(binIdentifier.FirstDayInBin.Year, 1, 1), binIdentifier.LastDayInBin, bin.ToArray());
                break;

            default:
                throw new NotImplementedException($"DataResolution {dataResolution}");
        }

        var buckets =
            cups
            .Select(
                x =>
                new Bucket
                {
                    FirstDayInBucket = x.FirstDayInCup,
                    LastDayInBucket = x.LastDayInCup,
                    Cups = [x],
                })
            .ToArray();

        return buckets;
    }

    private static bool DataRecordFallsInInclusiveRange(DataRecord dr, DateOnly start, DateOnly end)
    {
        DateOnly d = new (dr.Year, dr.Month ?? 1, dr.Day ?? 1);

        return d >= start && d <= end;
    }

    private static BinIdentifier GetBinIdentifier(DataRecord dp, BinGranularities binningRule)
    {
        // TODO: Notice that (for example) for a year with 365 data records, we will allocate 365 otherwise identical year bin identifiers.
        // We could cache them centrally and re-use to reduce allocations.
        return binningRule switch
        {
            BinGranularities.ByYear => new YearBinIdentifier(dp.Year),
            BinGranularities.ByYearAndMonth => new YearAndMonthBinIdentifier(dp.Year, dp.Month!.Value),
            BinGranularities.ByYearAndWeek => new YearAndWeekBinIdentifier(dp.Year, (short)GetIso8601WeekOfYear(new DateTime(dp.Year, dp.Month!.Value, dp.Day!.Value))),
            BinGranularities.ByYearAndDay => new YearAndDayBinIdentifier(dp.Year, dp.Month!.Value, dp.Day!.Value),
            BinGranularities.ByMonthOnly => new MonthOnlyBinIdentifier(dp.Month!.Value),
            BinGranularities.BySouthernHemisphereTemperateSeasonOnly => new SouthernHemisphereTemperateSeasonOnlyBinIdentifier(DateHelpers.GetSouthernHemisphereTemperateSeasonForMonth(dp.Month!.Value)),
            BinGranularities.BySouthernHemisphereTropicalSeasonOnly => new SouthernHemisphereTropicalSeasonOnlyBinIdentifier(DateHelpers.GetSouthernHemisphereTropicalSeasonForMonth(dp.Month!.Value)),
            _ => throw new NotImplementedException($"BinningRule {binningRule}"),
        };
    }
}
