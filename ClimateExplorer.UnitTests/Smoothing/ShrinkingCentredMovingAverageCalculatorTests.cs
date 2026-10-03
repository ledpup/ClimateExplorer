using System.Linq;
using ClimateExplorer.Core.Stats.Smoothing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class ShrinkingCentredMovingAverageCalculatorTests
{
    private const float Threshold = 0.75f;
    private const double Delta = 1e-9;

    private readonly ShrinkingCentredMovingAverageCalculator calculator = new();

    [TestMethod]
    [DataRow(21)]
    [DataRow(20)]
    [DataRow(3)]
    public void Smooth_PointsWithAFullWindow_MatchCentredMovingAverage(int windowSize)
    {
        var values = CreateValues(1960, 2026);

        var result = calculator.Smooth(values, windowSize, Threshold).Values;
        var centred = new CentredMovingAverageCalculator().Smooth(values, windowSize, Threshold).Values;

        for (int i = 0; i < values.Length; i++)
        {
            if (centred[i].HasValue)
            {
                Assert.AreEqual(centred[i]!.Value, result[i]!.Value, Delta, $"index {i}");
            }
        }
    }

    [TestMethod]
    [DataRow(2016, 2006, 2026)]
    [DataRow(2021, 2016, 2026)]
    [DataRow(2022, 2018, 2026)]
    [DataRow(2023, 2019, 2026)]
    [DataRow(2024, 2020, 2026)]
    [DataRow(2025, 2021, 2026)]
    [DataRow(2026, 2022, 2026)]
    public void Smooth_OddWindow21NearTrailingEnd_AveragesShrunkWindow(int year, int windowFirstYear, int windowLastYear)
    {
        var values = CreateValues(1960, 2026);

        var result = calculator.Smooth(values, 21, Threshold).Values;

        Assert.AreEqual(Mean(values, 1960, windowFirstYear, windowLastYear), result[year - 1960]!.Value, Delta);
    }

    [TestMethod]
    [DataRow(2017, 2007, 2026)]
    [DataRow(2018, 2009, 2026)]
    [DataRow(2021, 2015, 2026)]
    [DataRow(2022, 2017, 2026)]
    [DataRow(2023, 2018, 2026)]
    [DataRow(2026, 2021, 2026)]
    public void Smooth_EvenWindow20NearTrailingEnd_KeepsExtraSlotBeforePoint(int year, int windowFirstYear, int windowLastYear)
    {
        var values = CreateValues(1960, 2026);

        var result = calculator.Smooth(values, 20, Threshold).Values;

        Assert.AreEqual(Mean(values, 1960, windowFirstYear, windowLastYear), result[year - 1960]!.Value, Delta);
    }

    [TestMethod]
    [DataRow(1960, 1960, 1964)]
    [DataRow(1961, 1960, 1965)]
    [DataRow(1964, 1960, 1968)]
    [DataRow(1965, 1960, 1970)]
    [DataRow(1970, 1960, 1980)]
    public void Smooth_OddWindow21NearLeadingEnd_MirrorsTrailingEnd(int year, int windowFirstYear, int windowLastYear)
    {
        var values = CreateValues(1960, 2026);

        var result = calculator.Smooth(values, 21, Threshold).Values;

        Assert.AreEqual(Mean(values, 1960, windowFirstYear, windowLastYear), result[year - 1960]!.Value, Delta);
    }

    [TestMethod]
    public void Smooth_WindowNarrowerThanFloor_DoesNotWidenWindow()
    {
        var result = calculator.Smooth([1, 2, 3, 4, 5], 3, Threshold).Values;

        CollectionAssert.AreEqual(new double?[] { 1.5, 2, 3, 4, 4.5 }, result);
    }

    [TestMethod]
    public void Smooth_SeriesShorterThanWindow_GivesEveryPointAValue()
    {
        var result = calculator.Smooth([1, 2, 3, 4, 5], 21, Threshold).Values;

        Assert.IsTrue(result.All(x => x.HasValue));
    }

    [TestMethod]
    public void Smooth_LeadingAndTrailingNulls_ShrinksTowardsFirstAndLastValues()
    {
        double?[] values = [null, null, .. CreateValues(1960, 2026), null, null, null];

        var result = calculator.Smooth(values, 21, Threshold).Values;

        Assert.IsNull(result[0]);
        Assert.IsNull(result[1]);
        Assert.IsNull(result[^1]);
        Assert.IsNull(result[^2]);
        Assert.IsNull(result[^3]);

        // 1960 is at index 2, so index 0 stands for 1958.
        Assert.AreEqual(Mean(values, 1958, 1960, 1964), result[2]!.Value, Delta);
        Assert.AreEqual(Mean(values, 1958, 2022, 2026), result[^4]!.Value, Delta);
    }

    [TestMethod]
    public void Smooth_GapNearEndBelowThreshold_GivesNullForThatPointOnly()
    {
        var values = CreateValues(1960, 2026);
        values[2024 - 1960] = null;
        values[2025 - 1960] = null;

        var result = calculator.Smooth(values, 21, Threshold).Values;

        // 2026 uses 2022-2026: 3 of 5 slots. 2021 uses 2016-2026: 9 of 11 slots.
        Assert.IsNull(result[2026 - 1960]);
        Assert.IsNotNull(result[2021 - 1960]);
    }

    [TestMethod]
    public void Smooth_EmptyInput_GivesEmptyOutput()
    {
        Assert.IsEmpty(calculator.Smooth([], 21, Threshold).Values);
    }

    [TestMethod]
    public void Smooth_CurvedSeries_GivesDifferentValueForEachEndPoint()
    {
        var values = CreateValues(1960, 2026);

        var result = calculator.Smooth(values, 20, Threshold).Values;

        var lastTen = result.Skip(result.Length - 10).ToArray();
        Assert.HasCount(10, lastTen.Distinct().ToArray());
    }

    [TestMethod]
    public void Smooth_OddWindow21_ReportsWindowSizeUsedForEachPoint()
    {
        var values = CreateValues(1960, 2026);

        var windowSizes = calculator.Smooth(values, 21, Threshold).WindowSizes;

        // See the odd window table in Smooth_OddWindow21NearTrailingEnd_AveragesShrunkWindow.
        Assert.AreEqual(5, windowSizes[0]);
        Assert.AreEqual(21, windowSizes[2016 - 1960]);
        Assert.AreEqual(11, windowSizes[2021 - 1960]);
        Assert.AreEqual(9, windowSizes[2022 - 1960]);
        Assert.AreEqual(5, windowSizes[2026 - 1960]);
    }

    [TestMethod]
    public void Smooth_NullOutputs_ReportWindowSizeZero()
    {
        double?[] values = [null, .. CreateValues(1960, 2026)];
        values[^2] = null;
        values[^3] = null;

        var smoothed = calculator.Smooth(values, 21, Threshold);

        // Index 0 is outside the data. The last point fails the threshold (3 of 5 slots).
        Assert.AreEqual(0, smoothed.WindowSizes[0]);
        Assert.IsNull(smoothed.Values[^1]);
        Assert.AreEqual(0, smoothed.WindowSizes[^1]);
    }

    // A curved series, so that different windows give different means.
    private static double?[] CreateValues(int firstYear, int lastYear)
    {
        return Enumerable.Range(firstYear, lastYear - firstYear + 1)
            .Select(year => (double?)((year - firstYear) * (year - firstYear)))
            .ToArray();
    }

    private static double Mean(double?[] values, int yearOfIndex0, int windowFirstYear, int windowLastYear)
    {
        return values
            .Skip(windowFirstYear - yearOfIndex0)
            .Take(windowLastYear - windowFirstYear + 1)
            .Average()!.Value;
    }
}
