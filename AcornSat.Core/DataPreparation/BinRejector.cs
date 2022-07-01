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
            Console.WriteLine("bin " + bin.ToString());

            int fullEnoughBuckets = 0;

            foreach (var bucket in bin.Buckets)
            {
                Console.WriteLine("  bucket " + bucket.ToString());

                int fullEnoughCups = 0;

                foreach (var cup in bucket.Cups)
                {
                    Console.WriteLine("    cup " + cup.ToString());

                    var daysInCup = cup.DaysInCup;

                    var dataPointsInCup = cup.DataPoints.Count(x => x.Value.HasValue);

                    var proportionOfPointsInCup = (float)dataPointsInCup / daysInCup;

                    if (proportionOfPointsInCup >= requiredCupDataProportion)
                    {
                        fullEnoughCups++;
                    }
                    else
                    {
                        Console.WriteLine("        Cup has too few points");
                    }
                }

                Console.WriteLine("    ** BUCKET SUMMARY ** " + fullEnoughCups + " / " + bucket.Cups.Length + " = " + (float)fullEnoughCups / bucket.Cups.Length);

                if ((float)fullEnoughCups / bucket.Cups.Length >= requiredBucketDataProportion)
                {
                    fullEnoughBuckets++;
                }
            }

            Console.WriteLine("  ** BIN SUMMARY ** " + fullEnoughBuckets + " / " + bin.Buckets.Length + " = " + (float)fullEnoughBuckets / bin.Buckets.Length);
            if ((float)fullEnoughBuckets / bin.Buckets.Length >= requiredBinDataProportion)
            {
                return true;
            }

            return false;
        }
    }
}
