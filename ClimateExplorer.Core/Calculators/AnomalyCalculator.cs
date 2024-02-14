namespace ClimateExplorer.Core.Calculators;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;

public static class AnomalyCalculator
{
    public const int MinimumNumberOfYearsToCalculateAnomaly = 60;

    public static CalculatedAnomaly CalculateAnomaly(IEnumerable<DataRecord> dataRecords)
    {
        return
            CalculateAnomaly(
                dataRecords.Select(
                    x =>
                    new YearAndValue
                    {
                        Year = ((YearBinIdentifier)BinIdentifier.Parse(x.BinId!)).Year,
                        Value = x.Value,
                    })
                .ToArray());
    }

    public static CalculatedAnomaly CalculateAnomaly(ChartableDataPoint[] dataPoints)
    {
        return
            CalculateAnomaly(
                dataPoints.Select(
                    x =>
                    new YearAndValue
                    {
                        Year = ((YearBinIdentifier)BinIdentifier.Parse(x.BinId!)).Year,
                        Value = x.Value,
                    })
                .ToArray());
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Rule conflict")]
    private static CalculatedAnomaly CalculateAnomaly(YearAndValue[] dataPoints)
    {
        var nonNullDataPoints = dataPoints.Where(x => x.Value.HasValue).ToArray();

        if (nonNullDataPoints.Length < MinimumNumberOfYearsToCalculateAnomaly)
        {
            return null!;
        }

        var countOfFirstHalf = nonNullDataPoints.Length / 2;
        var firstHalf = nonNullDataPoints.OrderBy(x => x.Year).Take(countOfFirstHalf).ToArray();
        var averageOfFirstHalf = firstHalf.Average(x => x.Value)!.Value;
        var lastThirtyYears = nonNullDataPoints
                                                        .OrderByDescending(x => x.Year)
                                                        .Take(30)
                                                        .OrderBy(x => x.Year)
                                                        .ToArray();
        var averageOfLast30Years = lastThirtyYears.Average(x => x.Value)!.Value;

        return
            new CalculatedAnomaly
            {
                AnomalyValue = averageOfLast30Years - averageOfFirstHalf,
                AverageOfFirstHalf = averageOfFirstHalf,
                AverageOfLast30Years = averageOfLast30Years,
                CountOfFirstHalf = countOfFirstHalf,
                FirstYearInFirstHalf = firstHalf.First().Year,
                LastYearInFirstHalf = firstHalf.Last().Year,
                FirstYearInLast30Years = lastThirtyYears.First().Year,
                LastYearInLast30Years = lastThirtyYears.Last().Year,
            };
    }

    private class YearAndValue
    {
        public int Year { get; set; }
        public double? Value { get; set; }
    }
}

public class CalculatedAnomaly
{
    public double AnomalyValue { get; set; }
    public double AverageOfFirstHalf { get; set; }
    public int CountOfFirstHalf { get; set; }
    public double AverageOfLast30Years { get; set; }
    public int FirstYearInFirstHalf { get; set; }
    public int LastYearInFirstHalf { get; set; }
    public int FirstYearInLast30Years { get; set; }
    public int LastYearInLast30Years { get; set; }
}
