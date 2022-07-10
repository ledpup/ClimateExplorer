using System;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class BinRejector
    {
        public static RawBin[] ApplyBinRejectionRules(RawBin[] bins, float requiredCupDataProportion, float requiredBucketDataProportion, float requiredBinDataProportion)
        {
            return
                bins
                .Where(x => BinMeetsDataRequirements(x, requiredCupDataProportion, requiredBucketDataProportion, requiredBinDataProportion))
                .ToArray();
        }

        static bool BinMeetsDataRequirements(RawBin bin, float requiredCupDataProportion, float requiredBucketDataProportion, float requiredBinDataProportion)
        {
            int fullEnoughBuckets = 0;

            foreach (var bucket in bin.Buckets)
            {
                int fullEnoughCups = 0;

                foreach (var cup in bucket.Cups)
                {
                    var dataPointsInCup = cup.DataPoints.Count(x => x.Value.HasValue);

                    var proportionOfPointsInCup = (float)dataPointsInCup / cup.ExpectedDataPointsInCup;

                    if (proportionOfPointsInCup >= requiredCupDataProportion)
                    {
                        fullEnoughCups++;
                    }
                }

                if ((float)fullEnoughCups / bucket.Cups.Length >= requiredBucketDataProportion)
                {
                    fullEnoughBuckets++;
                }
            }

            if ((float)fullEnoughBuckets / bin.Buckets.Length >= requiredBinDataProportion)
            {
                return true;
            }

            return false;
        }
    }
}
