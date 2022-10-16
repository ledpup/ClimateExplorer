namespace ClimateExplorer.Core.DataPreparation
{
    public enum BinGranularities
    {
        ByYear,
        ByYearAndMonth,
        ByYearAndDay,
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
            switch (b)
            {
                case BinGranularities.ByYear: return "By year";
                case BinGranularities.ByYearAndMonth: return "By year and month";
                case BinGranularities.BySouthernHemisphereTemperateSeasonOnly: return "By season";
                case BinGranularities.BySouthernHemisphereTropicalSeasonOnly: return "By tropical season";
                case BinGranularities.ByMonthOnly: return "By month";
                default: throw new NotImplementedException($"BinGranularities {b}");
            }
        }
    }
}
