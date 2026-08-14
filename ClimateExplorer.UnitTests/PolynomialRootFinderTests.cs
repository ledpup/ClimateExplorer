using System;
using ClimateExplorer.Core.Stats;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class PolynomialRootFinderTests
{
    [TestMethod]
    public void FindRealRoots_Linear_ReturnsSingleRoot()
    {
        // 4 + 2x = 0 => x = -2.
        var roots = PolynomialRootFinder.FindRealRoots([4, 2]);

        Assert.HasCount(1, roots);
        Assert.AreEqual(-2, roots[0], 1e-9);
    }

    [TestMethod]
    public void FindRealRoots_QuadraticWithTwoRealRoots_ReturnsBothAscending()
    {
        // (x - 2)(x - 3) = x^2 - 5x + 6.
        var roots = PolynomialRootFinder.FindRealRoots([6, -5, 1]);

        Assert.HasCount(2, roots);
        Assert.AreEqual(2, roots[0], 1e-9);
        Assert.AreEqual(3, roots[1], 1e-9);
    }

    [TestMethod]
    public void FindRealRoots_QuadraticWithNoRealRoots_ReturnsEmpty()
    {
        // x^2 + 1 - never crosses zero.
        var roots = PolynomialRootFinder.FindRealRoots([1, 0, 1]);

        Assert.IsEmpty(roots);
    }

    [TestMethod]
    public void FindRealRoots_QuadraticWithRepeatedRoot_ReturnsSingleRoot()
    {
        // (x - 2)^2 = x^2 - 4x + 4.
        var roots = PolynomialRootFinder.FindRealRoots([4, -4, 1]);

        Assert.HasCount(1, roots);
        Assert.AreEqual(2, roots[0], 1e-9);
    }

    [TestMethod]
    public void FindRealRoots_CubicWithThreeRealRoots_ReturnsAllAscending()
    {
        // (x - 1)(x - 2)(x - 3) = x^3 - 6x^2 + 11x - 6.
        var roots = PolynomialRootFinder.FindRealRoots([-6, 11, -6, 1]);

        Assert.HasCount(3, roots);
        Assert.AreEqual(1, roots[0], 1e-6);
        Assert.AreEqual(2, roots[1], 1e-6);
        Assert.AreEqual(3, roots[2], 1e-6);
    }

    [TestMethod]
    public void FindRealRoots_CubicWithOneRealRoot_ReturnsSingleRoot()
    {
        // x^3 - 1 = (x - 1)(x^2 + x + 1), and x^2 + x + 1 has no real roots.
        var roots = PolynomialRootFinder.FindRealRoots([-1, 0, 0, 1]);

        Assert.HasCount(1, roots);
        Assert.AreEqual(1, roots[0], 1e-6);
    }

    [TestMethod]
    public void FindRealRoots_UnsupportedDegree_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PolynomialRootFinder.FindRealRoots([1, 1, 1, 1, 1]));
    }
}
