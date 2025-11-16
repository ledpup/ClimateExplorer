using ClimateExplorer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class AverageTests
{
    [TestMethod]
    public void SimpleMedianTest()
    {
        var values = new double[] { 1, 4, 7 };
        Assert.AreEqual(4, values.Median());
    }

    [TestMethod]
    [DataRow(new double[] { 9, 10, 12, 13, 13, 13, 15, 15, 16, 16, 18, 22, 23, 24, 24, 25 }, 15.5F)]
    [DataRow(new double[] { 1, 1, 2, 6, 6, 9 }, 4)]
    public void ComplexMedianTest(double[] values, double expected)
    {
        Assert.AreEqual(expected, values.Median());
    }

    [TestMethod]
    public void AverageWithNullTest()
    {
        var values = new double?[] { 9, 10, null, 13, 4 };
        var mean = values.Average();
        Assert.HasCount(5, values);
        Assert.AreEqual(9, mean); // This means that nulls are ignored when calculating mean
    }
}
