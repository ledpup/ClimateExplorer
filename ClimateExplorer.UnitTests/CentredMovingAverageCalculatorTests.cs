using ClimateExplorer.Core;
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