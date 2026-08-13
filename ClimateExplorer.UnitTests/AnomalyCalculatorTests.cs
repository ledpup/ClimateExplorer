namespace ClimateExplorer.UnitTests;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AnomalyCalculatorTests
{
    [TestMethod]
    public void CalculateAnomaly_FewerThanMinimumYears_ReturnsNull()
    {
        var records = BuildYearRange(1990, 2019, 10.0); // 30 years, below the 60-year minimum

        var anomaly = AnomalyCalculator.CalculateAnomaly(records);

        Assert.IsNull(anomaly);
    }

    [TestMethod]
    public void CalculateAnomaly_FullPeriodStatistics_ReflectOverallSpanAndGaps()
    {
        // 1900-1969 (70 calendar years), all years have data except 1930 and 1940.
        var records = BuildYearRange(1900, 1969, 10.0, skipYears: [1930, 1940]);

        var anomaly = AnomalyCalculator.CalculateAnomaly(records);

        Assert.IsNotNull(anomaly);
        Assert.AreEqual(1900, anomaly!.FirstYearOverall);
        Assert.AreEqual(1969, anomaly.LastYearOverall);
        Assert.AreEqual(68, anomaly.CountOfFullPeriod);
        Assert.AreEqual(2, anomaly.MissingYearsInFullPeriod);
        Assert.AreEqual(10.0, anomaly.AverageOfFullPeriod);
    }

    [TestMethod]
    public void CalculateAnomaly_FirstHalfAndLast30Years_OnlyCountGapsWithinTheirOwnSpan()
    {
        // 1900-1969 (70 calendar years), all years have data except 1930 and 1940.
        // First half = earliest 34 non-null years -> 1900-1934 (1930 is the only gap in that span).
        // Last 30 years = most recent 30 non-null years -> 1939-1969 (1940 is the only gap in that span).
        var records = BuildYearRange(1900, 1969, 10.0, skipYears: [1930, 1940]);

        var anomaly = AnomalyCalculator.CalculateAnomaly(records);

        Assert.IsNotNull(anomaly);
        Assert.AreEqual(1900, anomaly!.FirstYearInFirstHalf);
        Assert.AreEqual(1934, anomaly.LastYearInFirstHalf);
        Assert.AreEqual(34, anomaly.CountOfFirstHalf);
        Assert.AreEqual(1, anomaly.MissingYearsInFirstHalf);
        Assert.AreEqual(10.0, anomaly.AverageOfFirstHalf);

        Assert.AreEqual(1939, anomaly.FirstYearInLast30Years);
        Assert.AreEqual(1969, anomaly.LastYearInLast30Years);
        Assert.AreEqual(1, anomaly.MissingYearsInLast30Years);
        Assert.AreEqual(10.0, anomaly.AverageOfLast30Years);

        Assert.AreEqual(0.0, anomaly.AnomalyValue);
    }

    [TestMethod]
    public void CalculateAnomaly_NoGaps_ReportsZeroMissingYearsEverywhere()
    {
        var records = BuildYearRange(1950, 2019, 10.0); // 70 consecutive years, no gaps

        var anomaly = AnomalyCalculator.CalculateAnomaly(records);

        Assert.IsNotNull(anomaly);
        Assert.AreEqual(0, anomaly!.MissingYearsInFullPeriod);
        Assert.AreEqual(0, anomaly.MissingYearsInFirstHalf);
        Assert.AreEqual(0, anomaly.MissingYearsInLast30Years);
    }

    private static List<BinnedRecord> BuildYearRange(int firstYear, int lastYear, double value, IReadOnlyCollection<int>? skipYears = null)
    {
        skipYears ??= [];

        return
            Enumerable.Range(firstYear, lastYear - firstYear + 1)
            .Where(year => !skipYears.Contains(year))
            .Select(year => new BinnedRecord($"y{year}", value))
            .ToList();
    }
}
