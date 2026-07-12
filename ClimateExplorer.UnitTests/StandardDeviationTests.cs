using System;
using ClimateExplorer.Core.Stats;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class StandardDeviationTests
{
    // Wikipedia "Standard deviation" article, worked example under "Basic examples":
    // https://en.wikipedia.org/wiki/Standard_deviation#Basic_examples
    // Data set {2, 4, 4, 4, 5, 5, 7, 9}, mean 5.
    // Population variance = 32/8 = 4, population SD = 2.
    // Sample variance = 32/7 = 4.5714..., sample SD = sqrt(32/7) = 2.13808993529939...
    private static readonly double[] WikipediaExample = { 2, 4, 4, 4, 5, 5, 7, 9 };

    [TestMethod]
    public void PopulationStandardDeviation_WikipediaExample()
    {
        var result = StandardDeviationCalculator.PopulationStandardDeviation(WikipediaExample);

        Assert.AreEqual(2.0, result!.Value, 1e-9);
    }

    [TestMethod]
    public void SampleStandardDeviation_WikipediaExample()
    {
        var result = StandardDeviationCalculator.SampleStandardDeviation(WikipediaExample);

        Assert.AreEqual(2.13808993529939, result!.Value, 1e-9);
    }

    // Hand-calculable set {1, 2, 3, 4, 5}, mean 3, sum of squared deviations = 10.
    // Population variance = 10/5 = 2, population SD = sqrt(2) = 1.4142135623730951.
    // Sample variance = 10/4 = 2.5, sample SD = sqrt(2.5) = 1.5811388300841898.
    private static readonly double[] OneToFive = { 1, 2, 3, 4, 5 };

    [TestMethod]
    public void PopulationStandardDeviation_OneToFive()
    {
        var result = StandardDeviationCalculator.PopulationStandardDeviation(OneToFive);

        Assert.AreEqual(1.4142135623730951, result!.Value, 1e-12);
    }

    [TestMethod]
    public void SampleStandardDeviation_OneToFive()
    {
        var result = StandardDeviationCalculator.SampleStandardDeviation(OneToFive);

        Assert.AreEqual(1.5811388300841898, result!.Value, 1e-12);
    }

    [TestMethod]
    public void PopulationStandardDeviation_EmptyCollection_ReturnsNull()
    {
        var result = StandardDeviationCalculator.PopulationStandardDeviation(Array.Empty<double>());

        Assert.IsNull(result);
    }

    [TestMethod]
    public void PopulationStandardDeviation_SingleValue_IsZero()
    {
        var result = StandardDeviationCalculator.PopulationStandardDeviation(new double[] { 42 });

        Assert.AreEqual(0.0, result!.Value, 1e-12);
    }

    [TestMethod]
    public void PopulationStandardDeviation_AllValuesIdentical_IsZero()
    {
        var result = StandardDeviationCalculator.PopulationStandardDeviation(new double[] { 7, 7, 7, 7 });

        Assert.AreEqual(0.0, result!.Value, 1e-12);
    }

    [TestMethod]
    public void SampleStandardDeviation_EmptyCollection_ReturnsNull()
    {
        var result = StandardDeviationCalculator.SampleStandardDeviation(Array.Empty<double>());

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SampleStandardDeviation_SingleValue_ReturnsNull()
    {
        // n - 1 == 0, so a sample SD is undefined for a single observation.
        var result = StandardDeviationCalculator.SampleStandardDeviation(new double[] { 42 });

        Assert.IsNull(result);
    }

    [TestMethod]
    public void StandardDeviationsFromMean_WikipediaExample_ExtremeValue()
    {
        // Historical set mean is 5, population SD is 2 (see WikipediaExample above).
        // 9 is exactly 2 standard deviations above the mean.
        var result = StandardDeviationCalculator.StandardDeviationsFromMean(9, WikipediaExample);

        Assert.AreEqual(2.0, result!.Value, 1e-9);
    }

    [TestMethod]
    public void StandardDeviationsFromMean_BelowMean_IsNegative()
    {
        // 1 is exactly 2 standard deviations below the mean of 5.
        var result = StandardDeviationCalculator.StandardDeviationsFromMean(1, WikipediaExample);

        Assert.AreEqual(-2.0, result!.Value, 1e-9);
    }

    [TestMethod]
    public void StandardDeviationsFromMean_ValueEqualsMean_IsZero()
    {
        var result = StandardDeviationCalculator.StandardDeviationsFromMean(5, WikipediaExample);

        Assert.AreEqual(0.0, result!.Value, 1e-9);
    }

    [TestMethod]
    public void StandardDeviationsFromMean_EmptyHistory_ReturnsNull()
    {
        var result = StandardDeviationCalculator.StandardDeviationsFromMean(5, Array.Empty<double>());

        Assert.IsNull(result);
    }

    [TestMethod]
    public void StandardDeviationsFromMean_ZeroVariance_ReturnsNull()
    {
        // Standard deviation of the history is 0, so the z-score would be a division by zero.
        var result = StandardDeviationCalculator.StandardDeviationsFromMean(7, new double[] { 7, 7, 7 });

        Assert.IsNull(result);
    }
}
