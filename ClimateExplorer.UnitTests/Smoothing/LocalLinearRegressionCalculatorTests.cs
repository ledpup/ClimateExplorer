using System;
using ClimateExplorer.Core.Stats.Smoothing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

/// <summary>
/// Each point's value is read off a straight line fitted through the values in its window.
/// The comments show the window and the line, so the expected values can be checked by hand.
/// "_" is a missing (null) value.
/// </summary>
[TestClass]
public class LocalLinearRegressionCalculatorTests
{
    private const double Tolerance = 1e-9;

    [TestMethod]
    public void Smooth_StraightLine_ReturnsInputUnchangedIncludingEnds()
    {
        var result = Smooth([1, 2, 3, 4, 5, 6, 7, 8, 9], windowSize: 3);

        // Every window lies on the line y = x + 1, so every fitted value is the original value.
        // (A moving average over the first window [1 2 3] would give 2 for index 0, not 1.)
        AssertAreEqual([1, 2, 3, 4, 5, 6, 7, 8, 9], result);
    }

    [TestMethod]
    public void Smooth_StraightLineWithEvenWindow_ReturnsInputUnchanged()
    {
        var result = Smooth([1, 2, 3, 4, 5, 6], windowSize: 4);

        AssertAreEqual([1, 2, 3, 4, 5, 6], result);
    }

    [TestMethod]
    public void Smooth_OneWindow_GivesLineOfBestFitAtEachPoint()
    {
        var result = Smooth([1, 2, 6], windowSize: 3);

        // All three points use the window [1 2 6]. Its line of best fit is y = 0.5 + 2.5x:
        //   mean x = 1, mean y = 3, slope = ((-1 * -2) + (0 * -1) + (1 * 3)) / 2 = 2.5
        // index 0: 0.5    index 1: 3 (the mean)    index 2: 5.5
        AssertAreEqual([0.5, 3, 5.5], result);
    }

    [TestMethod]
    public void Smooth_PointCentredInOddWindow_EqualsMeanOfWindow()
    {
        var result = Smooth([3, 1, 4, 1, 5, 9, 2], windowSize: 3);

        // A line of best fit passes through (mean x, mean y), and a centred point is at mean x.
        // index 1: [3 1 4] = 8/3    index 2: [1 4 1] = 2    index 3: [4 1 5] = 10/3
        // index 4: [1 5 9] = 5      index 5: [5 9 2] = 16/3
        Assert.AreEqual(8 / 3.0, result[1]!.Value, Tolerance);
        Assert.AreEqual(2, result[2]!.Value, Tolerance);
        Assert.AreEqual(10 / 3.0, result[3]!.Value, Tolerance);
        Assert.AreEqual(5, result[4]!.Value, Tolerance);
        Assert.AreEqual(16 / 3.0, result[5]!.Value, Tolerance);
    }

    [TestMethod]
    public void Smooth_LastPointsShareOneWindow_EachGetsADifferentValue()
    {
        var result = Smooth([0, 0, 0, 0, 1, 2, 3, 4, 5, 6], windowSize: 7);

        // The last four points (index 6 to 9) all use the window [0 1 2 3 4 5 6], which lies on
        // the line y = x - 3. A moving average would give all four the same value (3).
        AssertAreEqual([3, 4, 5, 6], result[6..]);
    }

    [TestMethod]
    public void Smooth_InteriorGapBelowThreshold_GivesNullAroundGap()
    {
        var result = Smooth([1, 2, 3, null, 5, 6, 7], windowSize: 3);

        // Windows that include the gap have 2 of 3 values (67%), below the 75% threshold.
        AssertAreEqual([1, 2, null, null, null, 6, 7], result);
    }

    [TestMethod]
    public void Smooth_InteriorGapWithSofterThreshold_FitsLineThroughRemainingValues()
    {
        var result = Smooth([1, 2, 3, null, null, 6, 7], windowSize: 3, requiredDataThreshold: 0.6f);

        // index 2: [2 3 _] line through the two values gives 3
        // index 3: [3 _ _] 1 of 3, fails    index 4: [_ _ 6] fails
        // index 5: [_ 6 7] line through the two values gives 6
        AssertAreEqual([1, 2, 3, null, null, 6, 7], result);
    }

    [TestMethod]
    public void Smooth_MissingValueWithEnoughNeighbours_IsFilledFromLine()
    {
        var result = Smooth([1, 2, 3, null, 5, 6, 7, 8], windowSize: 5);

        // index 3: [2 3 _ 5 6] has 4 of 5 values (80%). They lie on y = x + 1, so the gap gets 4.
        Assert.AreEqual(4, result[3]!.Value, Tolerance);
    }

    [TestMethod]
    public void Smooth_WindowWithSingleValue_GivesThatValue()
    {
        var result = Smooth([1, null, null, 4, null, null, 7], windowSize: 3, requiredDataThreshold: 0.3f);

        // One value can't define a slope, so the line is flat through it.
        // index 0-1: [1 _ _]    index 2: [_ _ 4]    index 3: [_ 4 _]    index 4: [4 _ _]    index 5-6: [_ _ 7]
        AssertAreEqual([1, 1, 4, 4, 4, 7, 7], result);
    }

    [TestMethod]
    public void Smooth_LeadingAndTrailingNulls_BoundariesFollowFirstAndLastValues()
    {
        var result = Smooth([null, null, 1, 2, 3, 4, 5, null], windowSize: 3);

        // The data runs from index 2 to 6. Points outside it stay null.
        AssertAreEqual([null, null, 1, 2, 3, 4, 5, null], result);
    }

    [TestMethod]
    public void Smooth_DataShorterThanWindow_GivesAllNull()
    {
        var result = Smooth([1, 2], windowSize: 3);

        AssertAreEqual([null, null], result);
    }

    [TestMethod]
    public void Smooth_DataShorterThanWindowAfterTrimmingPadding_GivesAllNull()
    {
        var result = Smooth([null, 1, 2, null, null], windowSize: 3);

        AssertAreEqual([null, null, null, null, null], result);
    }

    [TestMethod]
    public void Smooth_Window1_ReturnsInputUnchanged()
    {
        var result = Smooth([1, null, 3], windowSize: 1);

        AssertAreEqual([1, null, 3], result);
    }

    [TestMethod]
    public void Smooth_AllNull_GivesAllNull()
    {
        var result = Smooth([null, null, null], windowSize: 3);

        AssertAreEqual([null, null, null], result);
    }

    [TestMethod]
    public void Smooth_EmptyInput_GivesEmptyOutput()
    {
        var result = Smooth([], windowSize: 3);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void Smooth_WindowSizeZero_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Smooth([1, 2, 3], windowSize: 0));
    }

    [TestMethod]
    [DataRow(0f)]
    [DataRow(1.01f)]
    public void Smooth_ThresholdOutOfRange_Throws(float requiredDataThreshold)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Smooth([1, 2, 3], windowSize: 3, requiredDataThreshold));
    }

    private static double?[] Smooth(double?[] values, int windowSize, float requiredDataThreshold = 0.75f)
    {
        return new LocalLinearRegressionCalculator().Smooth(values, windowSize, requiredDataThreshold);
    }

    private static void AssertAreEqual(double?[] expected, double?[] actual)
    {
        Assert.HasCount(expected.Length, actual);

        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] is double expectedValue)
            {
                Assert.IsNotNull(actual[i], $"Index {i}");
                Assert.AreEqual(expectedValue, actual[i]!.Value, Tolerance, $"Index {i}");
            }
            else
            {
                Assert.IsNull(actual[i], $"Index {i}");
            }
        }
    }
}
