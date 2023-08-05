using ClimateExplorer.Core.DataPreparation;

namespace ClimateExplorer.Core.Calculators;

public static class IndexCalculator
{
    class YearAndValue
    {
        public int Year { get; set; }
        public float? Value { get; set; }
    }

    public static CalculatedIndex CalculateIndex(IEnumerable<DataRecord> dataRecords)
    {
        return
            CalculateIndex(
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

    public static CalculatedIndex CalculateIndex(ChartableDataPoint[] dataPoints)
    {
        return
            CalculateIndex(
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

    static CalculatedIndex CalculateIndex(YearAndValue[] dataPoints)
    {
        var nonNullDataPoints = dataPoints.Where(x => x.Value.HasValue).ToArray();

        if (nonNullDataPoints.Length < 40)
        {
            return null;
        }

        var countOfFirstHalf = nonNullDataPoints.Length / 2;
        var firstHalf = nonNullDataPoints.OrderBy(x => x.Year).Take(countOfFirstHalf).ToArray();
        var averageOfFirstHalf = firstHalf.Average(x => x.Value).Value;
        var lastThirtyYears = nonNullDataPoints
                                                        .OrderByDescending(x => x.Year)
                                                        .Take(30)
                                                        .OrderBy(x => x.Year)
                                                        .ToArray();
        var averageOfLast30Years = lastThirtyYears.Average(x => x.Value).Value;

        return
            new CalculatedIndex
            {
                IndexValue = averageOfLast30Years - averageOfFirstHalf,
                AverageOfFirstHalf = averageOfFirstHalf,
                AverageOfLast30Years = averageOfLast30Years,
                CountOfFirstHalf = countOfFirstHalf,
                FirstYearInFirstHalf = firstHalf.First().Year,
                LastYearInFirstHalf = firstHalf.Last().Year,
                FirstYearInLast30Years = lastThirtyYears.First().Year,
                LastYearInLast30Years = lastThirtyYears.Last().Year
            };                
    }
}

public class CalculatedIndex
{
    public float IndexValue { get; set; }
    public float AverageOfFirstHalf { get; set; }
    public int CountOfFirstHalf { get; set; }
    public float AverageOfLast30Years { get; set; }
    public int FirstYearInFirstHalf { get; set; }
    public int LastYearInFirstHalf { get; set; }
    public int FirstYearInLast30Years { get; set; }
    public int LastYearInLast30Years { get; set; }
}
