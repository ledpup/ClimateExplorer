namespace ClimateExplorer.Core.DataPreparation
{
    public enum BinGranularities
    {
        ByYear,
        ByYearAndMonth,
        BySouthernHemisphereTemperateSeasonOnly,
        BySouthernHemisphereTropicalSeasonOnly,
        ByMonthOnly
    }

    public static class BinGranularityHelpers
    {
        public static bool IsLinear(this BinGranularities b)
        {
            return b == BinGranularities.ByYear || b == BinGranularities.ByYear;
        }

        public static bool IsModular(this BinGranularities b)
        {
            return !b.IsLinear();
        }
    }
}
