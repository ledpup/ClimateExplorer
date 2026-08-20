namespace ClimateExplorer.Core.Calculators;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public static class AnomalyCalculator
{
    public const int MinimumNumberOfYearsToCalculateAnomaly = 60;

    public static CalculatedAnomaly CalculateAnomaly(IEnumerable<BinnedRecord> dataRecords)
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

    public static string ValueAsString(this CalculatedAnomaly? calculatedAnomaly, UnitOfMeasure unitOfMeasure)
    {
        if (calculatedAnomaly == null)
        {
            return "NA";
        }

        return $"{(calculatedAnomaly.AnomalyValue >= 0 ? "+" : string.Empty)}{string.Format(unitOfMeasure == UnitOfMeasure.DegreesCelsius ? "{0:0.0}" : "{0:0}", calculatedAnomaly.AnomalyValue)}{(unitOfMeasure == UnitOfMeasure.DegreesCelsius ? "°C" : "mm")}";
    }

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
        var firstYearOverall = nonNullDataPoints.Min(x => x.Year);
        var lastYearOverall = nonNullDataPoints.Max(x => x.Year);
        var averageOfFullPeriod = nonNullDataPoints.Average(x => x.Value)!.Value;

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
                AverageOfFullPeriod = averageOfFullPeriod,
                CountOfFullPeriod = nonNullDataPoints.Length,
                FirstYearOverall = firstYearOverall,
                LastYearOverall = lastYearOverall,
                MissingYearsInFirstHalf = (firstHalf.Last().Year - firstHalf.First().Year + 1) - countOfFirstHalf,
                MissingYearsInLast30Years = (lastThirtyYears.Last().Year - lastThirtyYears.First().Year + 1) - lastThirtyYears.Length,
                MissingYearsInFullPeriod = (lastYearOverall - firstYearOverall + 1) - nonNullDataPoints.Length,
            };
    }

    private class YearAndValue
    {
        public int Year { get; set; }
        public double? Value { get; set; }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Not important")]
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

    /// <summary>Average across every non-null year in the dataset (not just the first or last halves).</summary>
    public double AverageOfFullPeriod { get; set; }
    public int CountOfFullPeriod { get; set; }
    public int FirstYearOverall { get; set; }
    public int LastYearOverall { get; set; }

    /// <summary>Years within [FirstYearInFirstHalf, LastYearInFirstHalf] with no data.</summary>
    public int MissingYearsInFirstHalf { get; set; }

    /// <summary>Years within [FirstYearInLast30Years, LastYearInLast30Years] with no data.</summary>
    public int MissingYearsInLast30Years { get; set; }

    /// <summary>Years within [FirstYearOverall, LastYearOverall] with no data.</summary>
    public int MissingYearsInFullPeriod { get; set; }
}
