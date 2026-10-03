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
    public void Smooth_EvenWindow4_HasExtraSlotBeforePoint()
    {
        var result = Smooth([1, 2, 3, 4, 5, 6], windowSize: 4);

        // index:   0     1     2          3          4          5
        // window:  none  none  [1 2 3 4]  [2 3 4 5]  [3 4 5 6]  none
        CollectionAssert.AreEqual(new double?[] { null, null, 2.5, 3.5, 4.5, null }, result);
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
