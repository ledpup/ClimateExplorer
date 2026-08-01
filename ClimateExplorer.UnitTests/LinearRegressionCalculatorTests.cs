using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class LinearRegressionCalculatorTests
{
    [TestMethod]
    public void Calculate_CanberraTemperatureAll_MatchesReferenceResults()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-1-canberra-temp-all.csv"));

        Assert.AreEqual(101, result.Input.Count);
        Assert.AreEqual(1914, result.Input.MinimumX);
        Assert.AreEqual(2025, result.Input.MaximumX);
        Assert.AreEqual(1974.08910891089, result.Input.MeanX, 1e-10);
        Assert.AreEqual(12.610297029703, result.Input.MeanY, 1e-12);
        Assert.AreEqual(99, result.Significance.DegreesOfFreedom);

        Assert.AreEqual(0.02043, result.Line.Slope, 5e-6);
        Assert.AreEqual(-27.73, result.Line.Intercept, 0.005);
        Assert.AreEqual(0.5638, result.Fit.RSquared, 5e-5);
        Assert.AreEqual(0.5569, result.Fit.ResidualStandardError, 5e-5);
        Assert.AreEqual(0.001807, result.Significance.SlopeStandardError, 5e-7);
        Assert.IsLessThan(0.0001, result.Significance.PValue);
        Assert.AreEqual(0.01685, result.Significance.SlopeConfidenceInterval.Lower, 5e-5);
        Assert.AreEqual(0.02402, result.Significance.SlopeConfidenceInterval.Upper, 5e-5);
        Assert.IsTrue(result.Significance.IsSlopeSignificant);
    }

    [TestMethod]
    public void Calculate_CanberraTemperatureLast30Years_MatchesReferenceResults()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-2-canberra-temp-last-30.csv"));

        Assert.AreEqual(30, result.Input.Count);
        Assert.AreEqual(2010.5, result.Input.MeanX, 1e-12);
        Assert.AreEqual(13.5693333333333, result.Input.MeanY, 1e-12);
        Assert.AreEqual(28, result.Significance.DegreesOfFreedom);

        Assert.AreEqual(0.03478, result.Line.Slope, 5e-6);
        Assert.AreEqual(-56.36, result.Line.Intercept, 0.005);
        Assert.AreEqual(0.2942, result.Fit.RSquared, 5e-5);
        Assert.AreEqual(0.4826, result.Fit.ResidualStandardError, 5e-5);
        Assert.AreEqual(0.01018, result.Significance.SlopeStandardError, 5e-5);
        Assert.AreEqual(0.0020, result.Significance.PValue, 5e-5);
        Assert.AreEqual(0.01393, result.Significance.SlopeConfidenceInterval.Lower, 5e-5);
        Assert.AreEqual(0.05563, result.Significance.SlopeConfidenceInterval.Upper, 5e-5);
        Assert.IsTrue(result.Significance.IsSlopeSignificant);
    }

    [TestMethod]
    public void Calculate_CanberraTemperatureFirstHalf_IsNotSignificant()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-3-canberra-temp-first-half.csv"));

        Assert.AreEqual(50, result.Input.Count);
        Assert.AreEqual(1947.66, result.Input.MeanX, 1e-12);
        Assert.AreEqual(12.0322, result.Input.MeanY, 1e-12);
        Assert.AreEqual(48, result.Significance.DegreesOfFreedom);

        Assert.AreEqual(0.003698, result.Line.Slope, 5e-7);
        Assert.AreEqual(4.830, result.Line.Intercept, 0.005);
        Assert.AreEqual(0.01755, result.Fit.RSquared, 5e-5);
        Assert.AreEqual(0.4863, result.Fit.ResidualStandardError, 5e-5);
        Assert.AreEqual(0.003994, result.Significance.SlopeStandardError, 5e-6);
        Assert.AreEqual(0.3591, result.Significance.PValue, 5e-5);
        Assert.AreEqual(-0.004332, result.Significance.SlopeConfidenceInterval.Lower, 5e-6);
        Assert.AreEqual(0.01173, result.Significance.SlopeConfidenceInterval.Upper, 5e-5);
        Assert.IsFalse(result.Significance.IsSlopeSignificant);
    }

    [TestMethod]
    public void Calculate_CanberraTemperatureAll_ResultsFileMatchesCalculatedRegression()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-1-canberra-temp-all.csv"));
        var expected = ReadResultFixture("Climate", "linear-regression-1-canberra-temp-all-results.txt");

        AssertMatchesResultFixture(expected, result);
    }

    [TestMethod]
    public void Calculate_CanberraTemperatureLast30Years_ResultsFileMatchesCalculatedRegression()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-2-canberra-temp-last-30.csv"));
        var expected = ReadResultFixture("Climate", "linear-regression-2-canberra-temp-last-30-results.txt");

        AssertMatchesResultFixture(expected, result);
    }

    [TestMethod]
    public void Calculate_CanberraTemperatureFirstHalf_ResultsFileMatchesCalculatedRegression()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-3-canberra-temp-first-half.csv"));
        var expected = ReadResultFixture("Climate", "linear-regression-3-canberra-temp-first-half-results.txt");

        AssertMatchesResultFixture(expected, result);
    }

    [TestMethod]
    public void Calculate_NistNorrisReferenceDataset_MatchesCertifiedValues()
    {
        // NIST/ITL StRD Norris, certified linear least squares values:
        // https://www.itl.nist.gov/div898/strd/lls/data/LINKS/v-Norris.shtml
        var result = LinearRegressionCalculator.Calculate(ReadFixture("Public", "nist-norris.csv"));

        Assert.AreEqual(36, result.Input.Count);
        Assert.AreEqual(-0.262323073774029, result.Line.Intercept, 1e-12);
        Assert.AreEqual(1.00211681802045, result.Line.Slope, 1e-13);
        Assert.AreEqual(0.000429796848199937, result.Significance.SlopeStandardError, 1e-15);
        Assert.AreEqual(0.884796396144373, result.Fit.ResidualStandardError, 1e-13);
        Assert.AreEqual(0.999993745883712, result.Fit.RSquared, 1e-15);
        Assert.AreEqual(26.6173985294224, result.Fit.ResidualSumOfSquares, 5e-12);
        Assert.AreEqual(5436385.54079785, result.Significance.FStatistic, 0.01);
        Assert.IsLessThan(1e-80, result.Significance.PValue);
    }

    [TestMethod]
    public void Calculate_WikipediaWomenHeightMassDataset_MatchesWorkedExample()
    {
        // Wikipedia "Simple linear regression" numerical example publishes the data,
        // slope, intercept, residual variance, slope standard-error variance, correlation,
        // and 95 percent slope confidence interval.
        var result = LinearRegressionCalculator.Calculate(ReadFixture("Public", "wikipedia-women-height-mass.csv"));

        Assert.AreEqual(15, result.Input.Count);
        Assert.AreEqual(1.65066666666667, result.Input.MeanX, 1e-14);
        Assert.AreEqual(62.078, result.Input.MeanY, 1e-12);
        Assert.AreEqual(61.272, result.Line.Slope, 0.001);
        Assert.AreEqual(-39.062, result.Line.Intercept, 0.001);
        Assert.AreEqual(Math.Sqrt(3.1539), result.Significance.SlopeStandardError, 0.0001);
        Assert.AreEqual(Math.Sqrt(0.5762), result.Fit.ResidualStandardError, 0.0001);
        Assert.AreEqual(0.9946 * 0.9946, result.Fit.RSquared, 0.0002);
        Assert.AreEqual(57.4, result.Significance.SlopeConfidenceInterval.Lower, 0.1);
        Assert.AreEqual(65.1, result.Significance.SlopeConfidenceInterval.Upper, 0.1);
        Assert.IsLessThan(1e-12, result.Significance.PValue);
    }

    [TestMethod]
    public void Calculate_PerfectPositiveLine_ReturnsExactLineAndCollapsedUncertainty()
    {
        var result = LinearRegressionCalculator.Calculate(
        [
            new DataPoint(1, 3),
            new DataPoint(2, 5),
            new DataPoint(3, 7),
            new DataPoint(4, 9),
        ]);

        Assert.AreEqual(2, result.Line.Slope, 1e-12);
        Assert.AreEqual(1, result.Line.Intercept, 1e-12);
        Assert.AreEqual(1, result.Fit.RSquared, 1e-12);
        Assert.AreEqual(0, result.Fit.ResidualStandardError, 1e-12);
        Assert.AreEqual(0, result.Significance.SlopeStandardError, 1e-12);
        Assert.IsTrue(double.IsPositiveInfinity(result.Significance.TStatistic));
        Assert.AreEqual(0, result.Significance.PValue, 1e-12);

        var prediction = LinearRegressionCalculator.Predict(result, 6);

        Assert.AreEqual(13, prediction.PredictedY, 1e-12);
        Assert.AreEqual(13, prediction.MeanConfidenceInterval.Lower, 1e-12);
        Assert.AreEqual(13, prediction.MeanConfidenceInterval.Upper, 1e-12);
        Assert.AreEqual(13, prediction.ObservationPredictionInterval.Lower, 1e-12);
        Assert.AreEqual(13, prediction.ObservationPredictionInterval.Upper, 1e-12);
    }

    [TestMethod]
    public void Calculate_PerfectNegativeLine_ReturnsNegativeSlope()
    {
        var result = LinearRegressionCalculator.Calculate(
        [
            new DataPoint(1, 8),
            new DataPoint(2, 6),
            new DataPoint(3, 4),
            new DataPoint(4, 2),
        ]);

        Assert.AreEqual(-2, result.Line.Slope, 1e-12);
        Assert.AreEqual(10, result.Line.Intercept, 1e-12);
        Assert.AreEqual(1, result.Fit.RSquared, 1e-12);
        Assert.IsTrue(double.IsNegativeInfinity(result.Significance.TStatistic));
        Assert.AreEqual(0, result.Significance.PValue, 1e-12);
        Assert.IsTrue(result.Significance.IsSlopeSignificant);
    }

    [TestMethod]
    public void Calculate_UnevenlySpacedXValues_UsesActualXSpacing()
    {
        var result = LinearRegressionCalculator.Calculate(
        [
            new DataPoint(0, 1),
            new DataPoint(2, 5),
            new DataPoint(5, 11),
            new DataPoint(11, 23),
        ]);

        Assert.AreEqual(2, result.Line.Slope, 1e-12);
        Assert.AreEqual(1, result.Line.Intercept, 1e-12);
        Assert.AreEqual(0, result.Fit.ResidualStandardError, 1e-12);
    }

    [TestMethod]
    public void Predict_NoisyRegression_ReturnsWiderObservationIntervalThanMeanInterval()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-2-canberra-temp-last-30.csv"));
        var prediction = LinearRegressionCalculator.Predict(result, 2026);

        var meanWidth = prediction.MeanConfidenceInterval.Upper - prediction.MeanConfidenceInterval.Lower;
        var observationWidth =
            prediction.ObservationPredictionInterval.Upper - prediction.ObservationPredictionInterval.Lower;

        Assert.AreEqual(result.Line.Predict(2026), prediction.PredictedY, 1e-12);
        Assert.IsGreaterThan(meanWidth, observationWidth);
        Assert.IsLessThan(prediction.PredictedY, prediction.MeanConfidenceInterval.Lower);
        Assert.IsGreaterThan(prediction.PredictedY, prediction.MeanConfidenceInterval.Upper);
    }

    [TestMethod]
    public void CalculateInterceptStatistics_CanberraTemperatureAll_MatchesReferenceResults()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-1-canberra-temp-all.csv"));

        var intercept = LinearRegressionCalculator.CalculateInterceptStatistics(result);

        Assert.AreEqual(-27.73, result.Line.Intercept, 0.005);
        Assert.AreEqual(-34.81, intercept.ConfidenceInterval.Lower, 0.05);
        Assert.AreEqual(-20.65, intercept.ConfidenceInterval.Upper, 0.05);
    }

    [TestMethod]
    public void CalculateXIntercept_CanberraTemperatureAll_MatchesReferenceResults()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-1-canberra-temp-all.csv"));

        var xIntercept = LinearRegressionCalculator.CalculateXIntercept(result);

        Assert.AreEqual(1357, xIntercept.Value, 1);
        Assert.IsNotNull(xIntercept.ConfidenceInterval);
        Assert.AreEqual(1226, xIntercept.ConfidenceInterval.Lower, 5);
        Assert.AreEqual(1449, xIntercept.ConfidenceInterval.Upper, 5);
    }

    [TestMethod]
    public void CalculateXIntercept_NotSignificantTrend_ReturnsNullConfidenceInterval()
    {
        var result = LinearRegressionCalculator.Calculate(
            ReadFixture("Climate", "linear-regression-3-canberra-temp-first-half.csv"));

        Assert.IsFalse(result.Significance.IsSlopeSignificant);

        var xIntercept = LinearRegressionCalculator.CalculateXIntercept(result);

        Assert.IsNull(xIntercept.ConfidenceInterval);
        Assert.AreEqual(-result.Line.Intercept / result.Line.Slope, xIntercept.Value, 1e-9);
    }

    [TestMethod]
    public void CalculateInterceptStatistics_PerfectPositiveLine_ReturnsCollapsedInterval()
    {
        var result = LinearRegressionCalculator.Calculate(
        [
            new DataPoint(1, 3),
            new DataPoint(2, 5),
            new DataPoint(3, 7),
            new DataPoint(4, 9),
        ]);

        var intercept = LinearRegressionCalculator.CalculateInterceptStatistics(result);

        Assert.AreEqual(0, intercept.StandardError, 1e-12);
        Assert.AreEqual(1, intercept.ConfidenceInterval.Lower, 1e-12);
        Assert.AreEqual(1, intercept.ConfidenceInterval.Upper, 1e-12);
    }

    [TestMethod]
    public void CalculateInterceptStatistics_NullRegression_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => LinearRegressionCalculator.CalculateInterceptStatistics(null!));
    }

    [TestMethod]
    public void CalculateXIntercept_NullRegression_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => LinearRegressionCalculator.CalculateXIntercept(null!));
    }

    [TestMethod]
    public void Calculate_NullPoints_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => LinearRegressionCalculator.Calculate(null!));
    }

    [TestMethod]
    public void Calculate_FewerThanThreePoints_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => LinearRegressionCalculator.Calculate(
            [
                new DataPoint(1, 1),
                new DataPoint(2, 2),
            ]));
    }

    [TestMethod]
    public void Calculate_NonFiniteValues_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => LinearRegressionCalculator.Calculate(
            [
                new DataPoint(1, 1),
                new DataPoint(2, double.NaN),
                new DataPoint(3, 3),
            ]));
    }

    [TestMethod]
    public void Calculate_AllXValuesSame_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => LinearRegressionCalculator.Calculate(
            [
                new DataPoint(5, 1),
                new DataPoint(5, 2),
                new DataPoint(5, 3),
            ]));
    }

    [TestMethod]
    public void Calculate_InvalidAlpha_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => LinearRegressionCalculator.Calculate(
            [
                new DataPoint(1, 1),
                new DataPoint(2, 2),
                new DataPoint(3, 3),
            ], 0));
    }

    [TestMethod]
    public void Predict_InvalidAlpha_ThrowsArgumentOutOfRangeException()
    {
        var result = LinearRegressionCalculator.Calculate(
        [
            new DataPoint(1, 1),
            new DataPoint(2, 2),
            new DataPoint(3, 3),
        ]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => LinearRegressionCalculator.Predict(result, 4, 1));
    }

    private static IReadOnlyList<DataPoint> ReadFixture(params string[] fixturePath)
    {
        var pathParts = new[] { AppContext.BaseDirectory, "LinearRegressionFixtures" }
            .Concat(fixturePath)
            .ToArray();
        var path = Path.Combine(pathParts);
        var points = new List<DataPoint>();

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var fields = line.Split(',');

            if (fields[0].Equals("x", StringComparison.OrdinalIgnoreCase)
                || fields.Length < 2
                || string.IsNullOrWhiteSpace(fields[1]))
            {
                continue;
            }

            points.Add(new DataPoint(
                double.Parse(fields[0], CultureInfo.InvariantCulture),
                double.Parse(fields[1], CultureInfo.InvariantCulture)));
        }

        return points;
    }

    private static RegressionExpectedResults ReadResultFixture(params string[] fixturePath)
    {
        var text = File.ReadAllText(GetFixturePath(fixturePath));
        var degreesOfFreedomFields = ReadTextField(text, "DFn,DFd").Split(',');
        var pValueText = ReadTextField(text, "P Value");

        return new RegressionExpectedResults(
            ReadFirstNumber(ReadTextField(text, "Slope")),
            ReadSecondNumber(ReadTextField(text, "Slope")),
            ReadFirstNumber(ReadTextField(text, "Y-intercept")),
            ReadFirstNumber(ReadTextField(text, "R Square")),
            ReadFirstNumber(ReadTextField(text, "Sy.x")),
            ReadFirstNumber(ReadTextField(text, "F")),
            int.Parse(degreesOfFreedomFields[1], CultureInfo.InvariantCulture),
            ParsePValue(pValueText),
            pValueText.TrimStart().StartsWith('<'),
            ReadRange(ReadTextField(text, "Slope", occurrence: 2)),
            ReadFirstNumber(ReadTextField(text, "Number of X values")),
            ReadFirstNumber(ReadTextField(text, "Total number of values")));
    }

    private static void AssertMatchesResultFixture(
        RegressionExpectedResults expected,
        LinearRegressionResult actual)
    {
        Assert.AreEqual(expected.Count, actual.Input.Count);
        Assert.AreEqual(expected.TotalValueCount, actual.Input.Count);
        Assert.AreEqual(expected.DegreesOfFreedom, actual.Significance.DegreesOfFreedom);
        Assert.AreEqual(expected.Slope, actual.Line.Slope, Tolerance(expected.Slope));
        Assert.AreEqual(expected.SlopeStandardError, actual.Significance.SlopeStandardError, Tolerance(expected.SlopeStandardError));
        Assert.AreEqual(expected.Intercept, actual.Line.Intercept, Tolerance(expected.Intercept));
        Assert.AreEqual(expected.RSquared, actual.Fit.RSquared, Tolerance(expected.RSquared));
        Assert.AreEqual(expected.ResidualStandardError, actual.Fit.ResidualStandardError, Tolerance(expected.ResidualStandardError));
        Assert.AreEqual(expected.FStatistic, actual.Significance.FStatistic, Tolerance(expected.FStatistic));
        Assert.AreEqual(expected.SlopeConfidenceInterval.Lower, actual.Significance.SlopeConfidenceInterval.Lower, Tolerance(expected.SlopeConfidenceInterval.Lower));
        Assert.AreEqual(expected.SlopeConfidenceInterval.Upper, actual.Significance.SlopeConfidenceInterval.Upper, Tolerance(expected.SlopeConfidenceInterval.Upper));

        if (expected.IsPValueUpperBound)
        {
            Assert.IsLessThan(expected.PValue, actual.Significance.PValue);
        }
        else
        {
            Assert.AreEqual(expected.PValue, actual.Significance.PValue, PValueTolerance(expected.PValue));
        }
    }

    private static string GetFixturePath(params string[] fixturePath)
    {
        var pathParts = new[] { AppContext.BaseDirectory, "LinearRegressionFixtures" }
            .Concat(fixturePath)
            .ToArray();

        return Path.Combine(pathParts);
    }

    private static string ReadTextField(string text, string label, int occurrence = 1)
    {
        var matches = Regex.Matches(
            text,
            @$"^{Regex.Escape(label)}\s+(?<value>.+)$",
            RegexOptions.Multiline);

        return matches[occurrence - 1].Groups["value"].Value.Trim();
    }

    private static ConfidenceInterval ReadRange(string value)
    {
        var numbers = Regex.Matches(value, @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))
            .ToArray();

        return new ConfidenceInterval(numbers[0], numbers[1]);
    }

    private static double ReadFirstNumber(string value) => ReadNumber(value, 0);

    private static double ReadSecondNumber(string value) => ReadNumber(value, 1);

    private static double ReadNumber(string value, int index)
    {
        var matches = Regex.Matches(value, @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?");

        return double.Parse(matches[index].Value, CultureInfo.InvariantCulture);
    }

    private static double ParsePValue(string value)
    {
        return double.Parse(
            value.Replace("<", string.Empty, StringComparison.Ordinal).Trim(),
            CultureInfo.InvariantCulture);
    }

    private static double Tolerance(double expected)
    {
        return Math.Max(Math.Abs(expected) * 5e-4, 5e-7);
    }

    private static double PValueTolerance(double expected)
    {
        return Math.Max(Math.Abs(expected) * 5e-4, 5e-5);
    }

    private sealed record RegressionExpectedResults(
        double Slope,
        double SlopeStandardError,
        double Intercept,
        double RSquared,
        double ResidualStandardError,
        double FStatistic,
        int DegreesOfFreedom,
        double PValue,
        bool IsPValueUpperBound,
        ConfidenceInterval SlopeConfidenceInterval,
        double Count,
        double TotalValueCount);
}
