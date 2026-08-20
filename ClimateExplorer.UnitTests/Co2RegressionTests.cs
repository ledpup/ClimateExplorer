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
/// Linear vs. quadratic regression on the same real-world window, predicted to the same future year -
/// fits the chart's "RecentDecade" (last 10 years) trend window over Mauna Loa annual mean CO2, at
/// degree 1 and degree 2, and projects both to 2100 so the two are directly comparable. See the
/// design doc (docs/design/2026-08-14-01-polynomial-regression-chart-trends.md) for how the fixture
/// was derived from the bundled NOAA source file.
/// </summary>
[TestClass]
public class Co2RegressionTests
{
    // The bundled co2_mm_mlo.txt runs from March 1958 to mid-2026, so the last complete calendar
    // year is 2025 and the RecentDecade (last-10-years) window is 2016-2025 - matching
    // ChartSeriesTrendCalculator.RecentDecadeWindowYears exactly.
    private const int WindowStartYear = 2016;
    private const int WindowEndYear = 2025;
    private const int PredictionYear = 2100;

    [TestMethod]
    public void Calculate_Co2RecentDecadeLinear_IsSignificant()
    {
        var result = PolynomialRegressionCalculator.Calculate(ReadWindowPoints(), degree: 1);

        Assert.AreEqual(10, result.Input.Count);
        Assert.IsTrue(result.Significance.IsSlopeSignificant);
    }

    [TestMethod]
    public void Calculate_Co2RecentDecadeQuadratic_IsSignificant()
    {
        var result = PolynomialRegressionCalculator.Calculate(ReadWindowPoints(), degree: 2);

        Assert.AreEqual(10, result.Input.Count);
        Assert.IsTrue(
            result.Significance.IsSlopeSignificant,
            $"Expected the quadratic fit over {WindowStartYear}-{WindowEndYear} to be a statistically significant improvement "
            + $"over a flat line (p = {result.Significance.PValue}) - real Mauna Loa CO2 growth is visibly accelerating over this window.");
    }

    [TestMethod]
    public void Predict_Co2RecentDecadeQuadraticTo2100_FallsWithinPlausibleRange()
    {
        var linear = PolynomialRegressionCalculator.Calculate(ReadWindowPoints(), degree: 1);
        var quadratic = PolynomialRegressionCalculator.Calculate(ReadWindowPoints(), degree: 2);

        var linearPrediction = PolynomialRegressionCalculator.Predict(linear, PredictionYear);
        var quadraticPrediction = PolynomialRegressionCalculator.Predict(quadratic, PredictionYear);

        Console.WriteLine(
            $"Linear {WindowStartYear}-{WindowEndYear} fit: Y = {linear.Curve.Coefficients[0].ToString("0.####", CultureInfo.InvariantCulture)} + "
            + $"{linear.Curve.Coefficients[1].ToString("0.####", CultureInfo.InvariantCulture)}*X");
        Console.WriteLine(
            $"Quadratic {WindowStartYear}-{WindowEndYear} fit: Y = {quadratic.Curve.Coefficients[0].ToString("0.####", CultureInfo.InvariantCulture)} + "
            + $"{quadratic.Curve.Coefficients[1].ToString("0.####", CultureInfo.InvariantCulture)}*X + "
            + $"{quadratic.Curve.Coefficients[2].ToString("0.######", CultureInfo.InvariantCulture)}*X^2");
        Console.WriteLine($"Linear {PredictionYear} prediction: {linearPrediction.PredictedY.ToString("0.0", CultureInfo.InvariantCulture)} ppm");
        Console.WriteLine($"Quadratic {PredictionYear} prediction: {quadraticPrediction.PredictedY.ToString("0.0", CultureInfo.InvariantCulture)} ppm");

        // A curve fitted to an accelerating decade should curve upward faster than a straight line
        // through the same decade, once both are extrapolated 75 years past the end of the window.
        Assert.IsGreaterThan(linearPrediction.PredictedY, quadraticPrediction.PredictedY);

        // Wide sanity band, not a tight regression guard - this is a 75-year extrapolation from a
        // 10-year window, so the curvature in the fitted decade gets amplified a long way past it.
        // The actual computed value here is ~866 ppm: real Mauna Loa growth accelerated enough over
        // 2016-2025 that a quadratic fitted to just those ten points, extended to 2100, lands well
        // above a straight-line extrapolation of the same decade (~616 ppm) - high but not absurd
        // (within RCP8.5-scenario territory). The band exists to catch a badly broken fit (negative,
        // below the linear prediction, NaN/Infinity), not to pin today's exact figure.
        Assert.IsGreaterThanOrEqualTo(550, quadraticPrediction.PredictedY);
        Assert.IsLessThanOrEqualTo(950, quadraticPrediction.PredictedY);
    }

    private static IReadOnlyList<DataPoint> ReadWindowPoints()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "PolynomialRegressionFixtures", "Public", "co2-mauna-loa-annual-mean.csv");
        var points = new List<DataPoint>();

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("x,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fields = line.Split(',');
            var year = double.Parse(fields[0], CultureInfo.InvariantCulture);

            if (year is >= WindowStartYear and <= WindowEndYear)
            {
                points.Add(new DataPoint(year, double.Parse(fields[1], CultureInfo.InvariantCulture)));
            }
        }

        return points;
    }
}
