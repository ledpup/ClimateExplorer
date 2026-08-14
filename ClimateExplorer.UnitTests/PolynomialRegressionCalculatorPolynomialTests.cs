using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

/// <summary>
/// Degree 2 (quadratic) and degree 3 (cubic) coverage for <see cref="PolynomialRegressionCalculator"/>.
/// Degree 1 behaviour is covered - and proven byte-for-byte unchanged - by
/// <see cref="PolynomialRegressionCalculatorTests"/>, which is the original
/// <c>LinearRegressionCalculatorTests</c> with only mechanical renames.
/// </summary>
[TestClass]
public class PolynomialRegressionCalculatorPolynomialTests
{
    [TestMethod]
    public void Calculate_ExactQuadratic_RecoversCoefficientsAndPerfectFit()
    {
        // y = 2 + 3x + 0.5x^2, no noise - the primary correctness proof for the new maths, since it
        // doesn't depend on any external reference.
        var points = Enumerable.Range(-3, 9)
            .Select(x => new DataPoint(x, 2 + (3 * x) + (0.5 * x * x)))
            .ToList();

        var result = PolynomialRegressionCalculator.Calculate(points, degree: 2);

        Assert.AreEqual(2, result.Degree);
        Assert.AreEqual(2, result.Curve.Coefficients[0], 1e-9);
        Assert.AreEqual(3, result.Curve.Coefficients[1], 1e-9);
        Assert.AreEqual(0.5, result.Curve.Coefficients[2], 1e-9);
        Assert.AreEqual(1, result.Fit.RSquared, 1e-9);
        Assert.AreEqual(0, result.Fit.ResidualStandardError, 1e-9);
        Assert.IsTrue(result.Significance.IsSlopeSignificant);

        // Predict/Derivative at an out-of-sample X.
        Assert.AreEqual(2 + (3 * 10) + (0.5 * 10 * 10), result.Curve.Predict(10), 1e-6);
        Assert.AreEqual(3 + (2 * 0.5 * 10), result.Curve.Derivative(10), 1e-6);
    }

    [TestMethod]
    public void Calculate_ExactCubic_RecoversCoefficientsAndPerfectFit()
    {
        // y = 1 - 2x + 0.5x^2 + 0.1x^3, no noise.
        var points = Enumerable.Range(0, 10)
            .Select(x => new DataPoint(x, 1 - (2 * x) + (0.5 * x * x) + (0.1 * x * x * x)))
            .ToList();

        var result = PolynomialRegressionCalculator.Calculate(points, degree: 3);

        Assert.AreEqual(3, result.Degree);
        Assert.AreEqual(1, result.Curve.Coefficients[0], 1e-6);
        Assert.AreEqual(-2, result.Curve.Coefficients[1], 1e-6);
        Assert.AreEqual(0.5, result.Curve.Coefficients[2], 1e-6);
        Assert.AreEqual(0.1, result.Curve.Coefficients[3], 1e-6);
        Assert.AreEqual(1, result.Fit.RSquared, 1e-9);
        Assert.AreEqual(0, result.Fit.ResidualStandardError, 1e-6);

        var predicted = PolynomialRegressionCalculator.Predict(result, 20);
        var expected = 1 - (2 * 20) + (0.5 * 20 * 20) + (0.1 * 20 * 20 * 20);
        Assert.AreEqual(expected, predicted.PredictedY, 1e-3);
    }

    [TestMethod]
    public void CalculateCoefficientStatistics_ExactQuadratic_ReturnsCollapsedUncertainty()
    {
        var points = Enumerable.Range(-3, 9)
            .Select(x => new DataPoint(x, 2 + (3 * x) + (0.5 * x * x)))
            .ToList();

        var result = PolynomialRegressionCalculator.Calculate(points, degree: 2);
        var stats = PolynomialRegressionCalculator.CalculateCoefficientStatistics(result);

        Assert.HasCount(3, stats);
        Assert.AreEqual(0, stats[0].Power);
        Assert.AreEqual(2, stats[0].Value, 1e-9);
        Assert.AreEqual(0, stats[0].StandardError, 1e-6);
        Assert.AreEqual(1, stats[1].Power);
        Assert.AreEqual(3, stats[1].Value, 1e-9);
        Assert.AreEqual(2, stats[2].Power);
        Assert.AreEqual(0.5, stats[2].Value, 1e-9);
    }

    [TestMethod]
    public void CalculateRateOfChange_ExactQuadratic_MatchesAnalyticDerivative()
    {
        // y = 2 + 3x + 0.5x^2 => dy/dx = 3 + x.
        var points = Enumerable.Range(-3, 9)
            .Select(x => new DataPoint(x, 2 + (3 * x) + (0.5 * x * x)))
            .ToList();

        var result = PolynomialRegressionCalculator.Calculate(points, degree: 2);

        var rateAtZero = PolynomialRegressionCalculator.CalculateRateOfChange(result, 0);
        Assert.AreEqual(3, rateAtZero.Rate, 1e-9);
        Assert.AreEqual(0, rateAtZero.StandardError, 1e-6);

        var rateAtTwo = PolynomialRegressionCalculator.CalculateRateOfChange(result, 2);
        Assert.AreEqual(5, rateAtTwo.Rate, 1e-9);
    }

    [TestMethod]
    public void Calculate_NistPontiusReferenceDataset_MatchesCertifiedQuadraticValues()
    {
        // NIST/ITL StRD Pontius (load cell calibration), certified quadratic least squares values:
        // https://www.itl.nist.gov/div898/strd/lls/data/Pontius.shtml
        var result = PolynomialRegressionCalculator.Calculate(ReadFixture("nist-pontius.csv"), degree: 2);

        Assert.AreEqual(40, result.Input.Count);
        Assert.AreEqual(0.673565789473684E-03, result.Curve.Coefficients[0], 5e-8);
        Assert.AreEqual(0.732059160401003E-06, result.Curve.Coefficients[1], 5e-11);
        Assert.AreEqual(-0.316081871345029E-14, result.Curve.Coefficients[2], 5e-18);
        Assert.AreEqual(0.205177424076185E-03, result.Fit.ResidualStandardError, 5e-8);
        Assert.AreEqual(0.999999900178537, result.Fit.RSquared, 1e-10);
        Assert.AreEqual(185330865.995752, result.Significance.FStatistic, 1);
        Assert.IsTrue(result.Significance.IsSlopeSignificant);
    }

    [TestMethod]
    public void Calculate_DegreeZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PolynomialRegressionCalculator.Calculate(SimplePoints(), degree: 0));
    }

    [TestMethod]
    public void Calculate_DegreeAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PolynomialRegressionCalculator.Calculate(SimplePoints(), degree: 4));
    }

    [TestMethod]
    public void Calculate_QuadraticWithTooFewPoints_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => PolynomialRegressionCalculator.Calculate(
            [
                new DataPoint(1, 1),
                new DataPoint(2, 2),
                new DataPoint(3, 3),
            ], degree: 2));
    }

    [TestMethod]
    public void Calculate_QuadraticWithTooFewDistinctXValues_ThrowsArgumentException()
    {
        // 5 points but only 2 distinct X values - not enough to determine a quadratic uniquely.
        Assert.ThrowsExactly<ArgumentException>(
            () => PolynomialRegressionCalculator.Calculate(
            [
                new DataPoint(1, 1),
                new DataPoint(1, 1.1),
                new DataPoint(2, 2),
                new DataPoint(2, 2.1),
                new DataPoint(1, 0.9),
            ], degree: 2));
    }

    [TestMethod]
    public void CalculateXIntercept_QuadraticFit_ThrowsNotSupportedException()
    {
        var points = Enumerable.Range(-3, 9)
            .Select(x => new DataPoint(x, 2 + (3 * x) + (0.5 * x * x)))
            .ToList();
        var result = PolynomialRegressionCalculator.Calculate(points, degree: 2);

        Assert.ThrowsExactly<NotSupportedException>(
            () => PolynomialRegressionCalculator.CalculateXIntercept(result));
    }

    private static IReadOnlyList<DataPoint> SimplePoints() =>
    [
        new DataPoint(1, 1),
        new DataPoint(2, 2),
        new DataPoint(3, 3),
        new DataPoint(4, 4),
        new DataPoint(5, 5),
    ];

    private static IReadOnlyList<DataPoint> ReadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "PolynomialRegressionFixtures", "Public", fileName);
        var points = new List<DataPoint>();

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var fields = line.Split(',');

            if (fields[0].Equals("x", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            points.Add(new DataPoint(
                double.Parse(fields[0], CultureInfo.InvariantCulture),
                double.Parse(fields[1], CultureInfo.InvariantCulture)));
        }

        return points;
    }
}
