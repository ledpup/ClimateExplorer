namespace ClimateExplorer.Core.DataPreparation
{
    public enum BinGranularities
    {
        ByYear,
        ByYearAndDay,
        ByYearAndMonth,
        BySouthernHemisphereTemperateSeasonOnly,
        BySouthernHemisphereTropicalSeasonOnly,
        ByMonthOnly
    }

    public static class BinGranularityHelpers
    {
        public static bool IsLinear(this BinGranularities b)
        {
            return b == BinGranularities.ByYear || b == BinGranularities.ByYearAndMonth;
        }

        public static bool IsModular(this BinGranularities b)
        {
            return !b.IsLinear();
        }

        public static string ToFriendlyString(this BinGranularities b)
        {
            return b switch
            {
                BinGranularities.ByYear => "By year",
                BinGranularities.ByYearAndDay => "By year and day",
                BinGranularities.ByYearAndMonth => "By year and month",
                BinGranularities.BySouthernHemisphereTemperateSeasonOnly => "By season",
                BinGranularities.BySouthernHemisphereTropicalSeasonOnly => "By tropical season",
                BinGranularities.ByMonthOnly => "By month",
                _ => throw new NotImplementedException($"BinGranularities {b}"),
            };
        }
    }
}
