namespace ClimateExplorer.Core.DataPreparation;

public static class BinHelpers
{
    public static IEnumerable<BinIdentifier> EnumerateBinsInRange(BinIdentifier start, BinIdentifier end)
    {
        if (start is YearBinIdentifier ybiStart && end is YearBinIdentifier ybiEnd)
        {
            return ybiStart.EnumerateYearBinRangeUpTo(ybiEnd);
        }

        if (start is YearAndMonthBinIdentifier ymbiStart && end is YearAndMonthBinIdentifier ymbiEnd)
        {
            return ymbiStart.EnumerateYearAndMonthBinRangeUpTo(ymbiEnd);
        }

        if (start is YearAndWeekBinIdentifier ywbiStart && end is YearAndWeekBinIdentifier ywbiEnd)
        {
            return ywbiStart.EnumerateYearAndWeekBinRangeUpTo(ywbiEnd);
        }

        if (start is YearAndDayBinIdentifier ydbiStart && end is YearAndDayBinIdentifier ydbiEnd)
        {
            return ydbiStart.EnumerateYearAndDayBinRangeUpTo(ydbiEnd);
        }

        throw new Exception("Only supported for parameter pairs of type YearBinIdentifier or YearAndMonthBinIdentifier");
    }

    public static BinIdentifier[] GetMonthBins()
    {
        return
            Enumerable.Range(1, 12)
            .Select(x => new MonthOnlyBinIdentifier((short)x))
            .ToArray();
    }

    public static BinIdentifier[] GetDayBins()
    {
        var leapYearStart = new DateOnly(2000, 1, 1);
        return
            Enumerable.Range(0, 366)
            .Select(i => leapYearStart.AddDays(i))
            .Select(d => (BinIdentifier)new DayOnlyBinIdentifier((short)d.Month, (short)d.Day))
            .ToArray();
    }

    public static BinIdentifier[] GetSouthernHemisphereTemperateSeasonBins()
    {
        return
            [
                new SouthernHemisphereTemperateSeasonOnlyBinIdentifier(SouthernHemisphereTemperateSeasons.Summer),
                new SouthernHemisphereTemperateSeasonOnlyBinIdentifier(SouthernHemisphereTemperateSeasons.Autumn),
                new SouthernHemisphereTemperateSeasonOnlyBinIdentifier(SouthernHemisphereTemperateSeasons.Winter),
                new SouthernHemisphereTemperateSeasonOnlyBinIdentifier(SouthernHemisphereTemperateSeasons.Spring),
            ];
    }

    public static BinIdentifier[] GetTropicalSeasonBins()
    {
        return
            [
                new SouthernHemisphereTropicalSeasonOnlyBinIdentifier(SouthernHemisphereTropicalSeasons.Wet),
                new SouthernHemisphereTropicalSeasonOnlyBinIdentifier(SouthernHemisphereTropicalSeasons.Dry),
            ];
    }

    public static BinIdentifier[] GetBinsForModularGranularity(BinGranularities binGranularity)
    {
        return binGranularity switch
        {
            BinGranularities.ByMonthOnly => GetMonthBins(),
            BinGranularities.ByDayOnly => GetDayBins(),
            BinGranularities.BySouthernHemisphereTemperateSeasonOnly => GetSouthernHemisphereTemperateSeasonBins(),
            BinGranularities.BySouthernHemisphereTropicalSeasonOnly => GetTropicalSeasonBins(),
            _ => throw new NotImplementedException($"binGranularity {binGranularity}"),
        };
    }
}
