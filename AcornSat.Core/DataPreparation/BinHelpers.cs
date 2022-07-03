using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.DataPreparation
{
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

            throw new Exception("Only supported for parameter pairs of type YearBinIdentifier or YearAndMonthBinIdentifier");
        }
    }
}
