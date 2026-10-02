using ClimateExplorer.Core.Stats;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class CentredMovingAverageCalculatorTests
{
    [TestMethod]
    public void Window1GivesOriginalCollection()
    {
        var result = new double?[] { 1, 2, 3, 4, 5 }.CalculateCentredMovingAverage(1, 0.745f);

        CollectionAssert.AreEqual(new double?[] { 1, 2, 3, 4, 5 }, result.ToArray());
    }

    [TestMethod]
    public void Window1GivesOriginalCollectionWithSomeNulls()
    {
        var result = new double?[] { 1, null, null, 4, 5 }.CalculateCentredMovingAverage(1, 0.745f);

        CollectionAssert.AreEqual(new double?[] { 1, null, null, 4, 5 }, result.ToArray());
    }

    [TestMethod]
    public void Window1GivesOriginalCollectionWithAllNulls()
    {
        var result = new double?[] { null, null, null, null, null }.CalculateCentredMovingAverage(1, 0.745f);

        CollectionAssert.AreEqual(new double?[] { null, null, null, null, null }, result.ToArray());
    }

    [TestMethod]
    public void Window3GivesExpectedValues()
    {
        var result = new double?[] { 1, 2, 3, 4, 5 }.CalculateCentredMovingAverage(3, 0.745f);

        CollectionAssert.AreEqual(new double?[] { null, 2, 3, 4, null }, result.ToArray());
    }


    [TestMethod]
    public void CalculateCentredMovingAverage_EvenWindow4_HalfWeightsTheTwoEndPoints()
    {
        var result = new double?[] { 1, 2, 3, 4, 5, 6 }.CalculateCentredMovingAverage(4, 0.745f);

        // 2x4 moving average: 5 points with weights 0.5, 1, 1, 1, 0.5
        // index:   0     1     2            3            4     5
        // window:  none  none  [1 2 3 4 5]  [2 3 4 5 6]  none  none
        CollectionAssert.AreEqual(new double?[] { null, null, 3, 4, null, null }, result.ToArray());
    }

    [TestMethod]
    public void CalculateCentredMovingAverage_EvenWindow4WithNullEndPoint_AveragesOverPresentWeight()
    {
        var result = new double?[] { 8, 1, 1, 1, null }.CalculateCentredMovingAverage(4, 0.745f);

        // Present weight is 3.5 of 4 (0.875): ((0.5 * 8) + 1 + 1 + 1) / 3.5 = 2
        CollectionAssert.AreEqual(new double?[] { null, null, 2, null, null }, result.ToArray());
    }

    [TestMethod]
    public void CalculateCentredMovingAverage_EvenWindow4BelowThreshold_ReturnsNull()
    {
        var result = new double?[] { null, null, 1, 1, 8 }.CalculateCentredMovingAverage(4, 0.745f);

        // Present weight is 2.5 of 4 (0.625), below the threshold
        CollectionAssert.AreEqual(new double?[] { null, null, null, null, null }, result.ToArray());
    }

    [TestMethod]
    public void Window3GivesExpectedValuesAroundCentralNull()
    {
        var result = new double?[] { 1, 2, 3, null, 5, 6, 7 }.CalculateCentredMovingAverage(3, 0.745f);

        CollectionAssert.AreEqual(new double?[] { null, 2, null, null, null, 6, null }, result.ToArray());
    }

    [TestMethod]
    public void Window3GivesExpectedValuesAroundCentralNullWithSofterThreshold()
    {
        var result = new double?[] { 1, 2, 3, null, null, 6, 7 }.CalculateCentredMovingAverage(3, 0.6f);

        CollectionAssert.AreEqual(new double?[] { null, 2, 2.5f, null, null, 6.5f, null }, result.ToArray());
    }
}