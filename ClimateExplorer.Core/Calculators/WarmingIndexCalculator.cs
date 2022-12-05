using ClimateExplorer.Core.DataPreparation;

namespace ClimateExplorer.Core.Calculators
{
    public static class WarmingIndexCalculator
    {
        class YearAndValue
        {
            public int Year { get; set; }
            public float? Value { get; set; }
        }

        public static CalculatedWarmingIndex CalculateWarmingIndex(IEnumerable<DataRecord> dataRecords)
        {
            return
                CalculateWarmingIndex(
                    dataRecords.Select(
                        x =>
                        new YearAndValue
                        {
                            Year = ((YearBinIdentifier)BinIdentifier.Parse(x.BinId)).Year,
                            Value = x.Value
                        }
                    )
                    .ToArray()
                );
        }

        public static CalculatedWarmingIndex CalculateWarmingIndex(ChartableDataPoint[] dataPoints)
        {
            return
                CalculateWarmingIndex(
                    dataPoints.Select(
                        x =>
                        new YearAndValue
                        {
                            Year = ((YearBinIdentifier)BinIdentifier.Parse(x.BinId)).Year,
                            Value = x.Value
                        }
                    )
                    .ToArray()
                );
        }

        static CalculatedWarmingIndex CalculateWarmingIndex(YearAndValue[] dataPoints)
        {
            var nonNullDataPoints = dataPoints.Where(x => x.Value.HasValue).ToArray();

            if (nonNullDataPoints.Length < 40)
            {
                return null;
            }

            var countOfFirstHalfOfTemperatures = nonNullDataPoints.Length / 2;
            var firstHalfOfTemperatures = nonNullDataPoints.OrderBy(x => x.Year).Take(countOfFirstHalfOfTemperatures).ToArray();
            var averageOfFirstHalfOfTemperatures = firstHalfOfTemperatures.Average(x => x.Value).Value;
            var lastThirtyYearsOfTemperatures = nonNullDataPoints
                                                            .OrderByDescending(x => x.Year)
                                                            .Take(30)
                                                            .OrderBy(x => x.Year)
                                                            .ToArray();
            var averageOfLastTwentyYearsTemperatures = lastThirtyYearsOfTemperatures.Average(x => x.Value).Value;

            return
                new CalculatedWarmingIndex
                {
                    WarmingIndexValue = averageOfLastTwentyYearsTemperatures - averageOfFirstHalfOfTemperatures,
                    AverageOfFirstHalfOfTemperatures = averageOfFirstHalfOfTemperatures,
                    AverageOfLastTwentyYearsTemperatures = averageOfLastTwentyYearsTemperatures,
                    CountOfFirstHalfOfTemperatures = countOfFirstHalfOfTemperatures,
                    FirstYearInFirstHalfOfTemperatures = firstHalfOfTemperatures.First().Year,
                    LastYearInFirstHalfOfTemperatures = firstHalfOfTemperatures.Last().Year,
                    FirstYearInLast30YearsOfTemperatures = lastThirtyYearsOfTemperatures.First().Year,
                    LastYearInLast30YearsOfTemperatures = lastThirtyYearsOfTemperatures.Last().Year
                };                
        }
    }

    public class CalculatedWarmingIndex
    {
        public float WarmingIndexValue { get; set; }
        public float AverageOfFirstHalfOfTemperatures { get; set; }
        public int CountOfFirstHalfOfTemperatures { get; set; }
        public float AverageOfLastTwentyYearsTemperatures { get; set; }
        public int FirstYearInFirstHalfOfTemperatures { get; set; }
        public int LastYearInFirstHalfOfTemperatures { get; set; }
        public int FirstYearInLast30YearsOfTemperatures { get; set; }
        public int LastYearInLast30YearsOfTemperatures { get; set; }
    }
}
