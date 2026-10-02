using System;
using ClimateExplorer.Core.Stats.Smoothing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

/// <summary>
/// The comments above each assertion show the window used for each point, so the expected values
/// can be checked by hand. "_" is a missing (null) value.
/// </summary>
[TestClass]
public class BoundaryAdjustedMovingAverageCalculatorTests
{
    [TestMethod]
    public void Smooth_Window3_UsesForwardWindowAtStartAndTrailingWindowAtEnd()
    {
        var result = Smooth([1, 2, 3, 4, 5, 6, 7, 8, 9], windowSize: 3);

        // index:   0        1        2        ...  7        8
        // window:  [1 2 3]  [1 2 3]  [2 3 4]  ...  [7 8 9]  [7 8 9]
        //          forward  centred  centred       centred  trailing
        CollectionAssert.AreEqual(new double?[] { 2, 2, 3, 4, 5, 6, 7, 8, 8 }, result);
    }

    [TestMethod]
    public void Smooth_Window7_FirstAndLastFourPointsShareTheFirstAndLastSevenValues()
    {
        var result = Smooth([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], windowSize: 7);

        // index 0-3: [1 .. 7]  = 4   (0-2 forward, 3 centred)
        // index 4:   [2 .. 8]  = 5   (centred)
        // index 5:   [3 .. 9]  = 6   (centred)
        // index 6-9: [4 .. 10] = 7   (6 centred, 7-9 trailing)
        CollectionAssert.AreEqual(new double?[] { 4, 4, 4, 4, 5, 6, 7, 7, 7, 7 }, result);
    }

    [TestMethod]
    public void Smooth_EvenWindow4_HasExtraSlotBeforePointInMiddle()
    {
        var result = Smooth([1, 2, 3, 4, 5, 6], windowSize: 4);

        // index 0-2: [1 2 3 4] = 2.5
        // index 3:   [2 3 4 5] = 3.5   (two before, one after)
        // index 4-5: [3 4 5 6] = 4.5
        CollectionAssert.AreEqual(new double?[] { 2.5, 2.5, 2.5, 3.5, 4.5, 4.5 }, result);
    }

    [TestMethod]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(10)]
    public void Smooth_PointsWithFullCentredWindow_MatchCentredMovingAverage(int windowSize)
    {
        double?[] values =
        [
            13.9, 14.2, 13.7, null, 14.1, 14.4, 13.8, 14.0, 14.6, 14.3,
            null, null, 14.5, 14.2, 14.8, 14.7, 14.4, 15.0, null, 14.9,
            15.1, 14.8, 15.3, 15.2, 15.0, null, 15.4, 15.6, 15.3, 15.7,
        ];

        var centred = new CentredMovingAverageCalculator().Smooth(values, windowSize, 0.75f);
        var boundaryAdjusted = Smooth(values, windowSize);

        for (int i = 0; i < values.Length; i++)
        {
            if (centred[i].HasValue)
            {
                Assert.AreEqual(centred[i], boundaryAdjusted[i], $"Index {i}");
            }
        }
    }

    [TestMethod]
    public void Smooth_InteriorGapBelowThreshold_GivesNullAroundGap()
    {
        var result = Smooth([1, 2, 3, null, 5, 6, 7], windowSize: 3);

        // Windows that include the gap have 2 of 3 values (67%), below the 75% threshold.
        // index 0: [1 2 3] = 2    index 6: [5 6 7] = 6
        // (The centred moving average gives null at both ends: _, 2, _, _, _, 6, _)
        CollectionAssert.AreEqual(new double?[] { 2, 2, null, null, null, 6, 6 }, result);
    }

    [TestMethod]
    public void Smooth_InteriorGapWithSofterThreshold_AveragesRemainingValues()
    {
        var result = Smooth([1, 2, 3, null, null, 6, 7], windowSize: 3, requiredDataThreshold: 0.6f);

        // index 2: [2 3 _] = 2.5    index 3: [3 _ _] 1 of 3, fails    index 4: [_ _ 6] fails    index 5: [_ 6 7] = 6.5
        CollectionAssert.AreEqual(new double?[] { 2, 2, 2.5, null, null, 6.5, 6.5 }, result);
    }

    [TestMethod]
    public void Smooth_MissingValueWithEnoughNeighbours_IsFilledBySmoothedValue()
    {
        var result = Smooth([1, 2, 3, null, 5, 6, 7, 8], windowSize: 5);

        // index 3: [2 3 _ 5 6] has 4 of 5 values (80%), so it gets (2 + 3 + 5 + 6) / 4 = 4
        Assert.AreEqual(4, result[3]);
    }

    [TestMethod]
    public void Smooth_ProportionExactlyAtThreshold_GivesValue()
    {
        var result = Smooth([1, null, 3, 4], windowSize: 4);

        // Every point uses [1 _ 3 4]: 3 of 4 values is exactly 75%
        double expected = (1 + 3 + 4) / 3.0;
        CollectionAssert.AreEqual(new double?[] { expected, expected, expected, expected }, result);
    }

    [TestMethod]
    public void Smooth_LeadingAndTrailingNulls_BoundariesFollowFirstAndLastValues()
    {
        var result = Smooth([null, null, 1, 2, 3, 4, 5, null], windowSize: 3);

        // The data runs from index 2 to 6. Points outside it stay null.
        // index 2: [1 2 3] = 2 (forward)    index 6: [3 4 5] = 4 (trailing)
        CollectionAssert.AreEqual(new double?[] { null, null, 2, 2, 3, 4, 4, null }, result);
    }

    [TestMethod]
    public void Smooth_GapNearStart_ForwardWindowStillAppliesThreshold()
    {
        var result = Smooth([1, null, null, 4, 5, 6, 7], windowSize: 3, requiredDataThreshold: 0.6f);

        // index 0 and 1: [1 _ _] 1 of 3, fails    index 2: [_ _ 4] fails    index 3: [_ 4 5] = 4.5
        CollectionAssert.AreEqual(new double?[] { null, null, null, 4.5, 5, 6, 6 }, result);
    }

    [TestMethod]
    public void Smooth_DataShorterThanWindow_GivesAllNull()
    {
        var result = Smooth([1, 2], windowSize: 3);

        CollectionAssert.AreEqual(new double?[] { null, null }, result);
    }

    [TestMethod]
    public void Smooth_DataShorterThanWindowAfterTrimmingPadding_GivesAllNull()
    {
        var result = Smooth([null, 1, 2, null, null], windowSize: 3);

        CollectionAssert.AreEqual(new double?[] { null, null, null, null, null }, result);
    }

    [TestMethod]
    public void Smooth_Window1_ReturnsInputUnchanged()
    {
        var result = Smooth([1, null, 3], windowSize: 1);

        CollectionAssert.AreEqual(new double?[] { 1, null, 3 }, result);
    }

    [TestMethod]
    public void Smooth_AllNull_GivesAllNull()
    {
        var result = Smooth([null, null, null], windowSize: 3);

        CollectionAssert.AreEqual(new double?[] { null, null, null }, result);
    }

    [TestMethod]
    public void Smooth_EmptyInput_GivesEmptyOutput()
    {
        var result = Smooth([], windowSize: 3);

        Assert.AreEqual(0, result.Length);
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
        return new BoundaryAdjustedMovingAverageCalculator().Smooth(values, windowSize, requiredDataThreshold);
    }
}
