using ClimateExplorer.Core.Stats.Smoothing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class CentredMovingAverageCalculatorTests
{
    [TestMethod]
    public void Smooth_Window1_ReturnsInputUnchanged()
    {
        var result = Smooth([1, 2, 3, 4, 5], windowSize: 1);

        CollectionAssert.AreEqual(new double?[] { 1, 2, 3, 4, 5 }, result);
    }

    [TestMethod]
    public void Smooth_Window1WithSomeNulls_ReturnsInputUnchanged()
    {
        var result = Smooth([1, null, null, 4, 5], windowSize: 1);

        CollectionAssert.AreEqual(new double?[] { 1, null, null, 4, 5 }, result);
    }

    [TestMethod]
    public void Smooth_Window1WithAllNulls_ReturnsInputUnchanged()
    {
        var result = Smooth([null, null, null, null, null], windowSize: 1);

        CollectionAssert.AreEqual(new double?[] { null, null, null, null, null }, result);
    }

    [TestMethod]
    public void Smooth_Window3_GivesNullAtEachEnd()
    {
        var result = Smooth([1, 2, 3, 4, 5], windowSize: 3);

        // index:   0     1        2        3        4
        // window:  none  [1 2 3]  [2 3 4]  [3 4 5]  none
        CollectionAssert.AreEqual(new double?[] { null, 2, 3, 4, null }, result);
    }

    [TestMethod]
    public void Smooth_EvenWindow4_HalfWeightsTheTwoEndPoints()
    {
        var result = Smooth([1, 2, 3, 4, 5, 6], windowSize: 4, requiredDataThreshold: 0.745f);

        // 2x4 moving average: 5 points with weights 0.5, 1, 1, 1, 0.5
        // index:   0     1     2            3            4     5
        // window:  none  none  [1 2 3 4 5]  [2 3 4 5 6]  none  none
        CollectionAssert.AreEqual(new double?[] { null, null, 3, 4, null, null }, result);
    }

    [TestMethod]
    public void Smooth_EvenWindow4WithNullEndPoint_AveragesOverPresentWeight()
    {
        var result = Smooth([8, 1, 1, 1, null], windowSize: 4, requiredDataThreshold: 0.745f);

        // Present weight is 3.5 of 4 (0.875): ((0.5 * 8) + 1 + 1 + 1) / 3.5 = 2
        CollectionAssert.AreEqual(new double?[] { null, null, 2, null, null }, result);
    }

    [TestMethod]
    public void Smooth_EvenWindow4BelowThreshold_ReturnsNull()
    {
        var result = Smooth([null, null, 1, 1, 8], windowSize: 4, requiredDataThreshold: 0.745f);

        // Present weight is 2.5 of 4 (0.625), below the threshold
        CollectionAssert.AreEqual(new double?[] { null, null, null, null, null }, result);
    }

    [TestMethod]
    public void Smooth_EvenWindow4_ReportsRequestedWindowSizeForEveryValue()
    {
        var result = new CentredMovingAverageCalculator().Smooth([1, 2, 3, 4, 5, 6], 4, 0.75f);

        CollectionAssert.AreEqual(new[] { 0, 0, 4, 4, 0, 0 }, result.WindowSizes);
    }

    [TestMethod]
    public void Smooth_EvenWindow4_ReportsHalfWeightSlotsAsWindowStartAndEnd()
    {
        var result = new CentredMovingAverageCalculator().Smooth([1, 2, 3, 4, 5, 6], 4, 0.75f);

        // Index 2 averages slots 0-4 and index 3 slots 1-5, each with its two end slots at half weight.
        Assert.AreEqual(0, result.WindowStarts[2]);
        Assert.AreEqual(4, result.WindowEnds[2]);
        Assert.AreEqual(1, result.WindowStarts[3]);
        Assert.AreEqual(5, result.WindowEnds[3]);
    }

    [TestMethod]
    public void Smooth_Window3WithCentralNull_GivesNullAroundGap()
    {
        var result = Smooth([1, 2, 3, null, 5, 6, 7], windowSize: 3);

        // Each window touching the gap has 2 of 3 values (67%), below the 75% threshold.
        CollectionAssert.AreEqual(new double?[] { null, 2, null, null, null, 6, null }, result);
    }

    [TestMethod]
    public void Smooth_Window3WithCentralNullsAndSofterThreshold_AveragesRemainingValues()
    {
        var result = Smooth([1, 2, 3, null, null, 6, 7], windowSize: 3, requiredDataThreshold: 0.6f);

        // index 2: [2 3 _] = 2.5    index 3: [3 _ _] fails    index 4: [_ _ 6] fails    index 5: [_ 6 7] = 6.5
        CollectionAssert.AreEqual(new double?[] { null, 2, 2.5, null, null, 6.5, null }, result);
    }

    [TestMethod]
    public void Smooth_Window3_ReportsFullWindowForEveryValue()
    {
        var result = new CentredMovingAverageCalculator().Smooth([1, 2, 3, 4, 5], 3, 0.75f);

        CollectionAssert.AreEqual(new[] { 0, 3, 3, 3, 0 }, result.WindowSizes);
    }

    private static double?[] Smooth(double?[] values, int windowSize, float requiredDataThreshold = 0.75f)
    {
        return new CentredMovingAverageCalculator().Smooth(values, windowSize, requiredDataThreshold).Values;
    }
}
