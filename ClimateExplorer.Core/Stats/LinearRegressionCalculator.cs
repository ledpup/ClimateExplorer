namespace ClimateExplorer.Core.Stats;

using ClimateExplorer.Core.Stats.Model;
using static MathHelpers;

public static class LinearRegressionCalculator
{
    private const int MinimumPointCount = 3;

    public static LinearRegressionResult Calculate(IEnumerable<DataPoint> points, double alpha = 0.05)
    {
        ValidateAlpha(alpha);

        var observations = ValidatePoints(points);
        var input = CalculateInputSummary(observations);
        var line = CalculateBestFitLine(observations, input);
        var fit = CalculateFit(observations, input, line);
        var significance = CalculateSignificance(input, line, fit, alpha);

        return new LinearRegressionResult(input, line, fit, significance);
    }

    public static RegressionPrediction Predict(
        LinearRegressionResult regression,
        double x,
        double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(regression);
        ValidateAlpha(alpha);
        ValidateFinite(x, nameof(x));

        var predictedY = regression.Line.Predict(x);
        var leverage =
            (1.0 / regression.Input.Count)
            + (Square(x - regression.Input.MeanX) / regression.Input.SumSquaredXDeviations);

        var tCritical = StudentTDistributionCalculator.TwoTailedCriticalValue(
            alpha,
            regression.Significance.DegreesOfFreedom);

        var meanMargin = tCritical * regression.Fit.ResidualStandardError * Math.Sqrt(leverage);
        var observationMargin = tCritical * regression.Fit.ResidualStandardError * Math.Sqrt(1 + leverage);

        return new RegressionPrediction(
            x,
            predictedY,
            new ConfidenceInterval(predictedY - meanMargin, predictedY + meanMargin),
            new ConfidenceInterval(predictedY - observationMargin, predictedY + observationMargin),
            alpha);
    }

    /// <summary>
    /// The Y-intercept is the fitted mean at X = 0, so its standard error and confidence interval
    /// are exactly what <see cref="Predict"/> already computes at that point - the leverage term
    /// <c>(1/n) + (x - meanX)^2/Sxx</c> reduces to the textbook intercept-SE formula when x = 0.
    /// </summary>
    public static InterceptStatistics CalculateInterceptStatistics(LinearRegressionResult regression, double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(regression);
        ValidateAlpha(alpha);

        var prediction = Predict(regression, 0, alpha);
        var tCritical = StudentTDistributionCalculator.TwoTailedCriticalValue(alpha, regression.Significance.DegreesOfFreedom);
        var marginOfError = prediction.MeanConfidenceInterval.Upper - prediction.PredictedY;
        var standardError = marginOfError / tCritical;

        return new InterceptStatistics(standardError, prediction.MeanConfidenceInterval);
    }

    /// <summary>
    /// The X-intercept is the X where the fitted line crosses Y = 0. Its point estimate is a simple
    /// ratio (-intercept/slope), but because the intercept and slope are correlated estimates, its
    /// confidence interval is not a simple ratio of their own intervals. This uses Fieller's theorem
    /// (Fieller, 1954; Draper &amp; Smith, <i>Applied Regression Analysis</i>, S5.3), equivalent to
    /// finding the X values where the fitted-mean confidence band (see <see cref="Predict"/>) touches
    /// zero: solving <c>(intercept + slope*X)^2 = (t*s)^2 * (1/n + (X-meanX)^2/Sxx)</c> for X. This is
    /// a quadratic in X whose leading coefficient is <c>slope^2 * (1-g)</c>, where
    /// <c>g = (t*s)^2 / (slope^2 * Sxx)</c>; when g is not less than 1 (the slope is not estimated
    /// precisely enough relative to its own size) the confidence interval is unbounded, so
    /// <see cref="XInterceptStatistics.ConfidenceInterval"/> is <see langword="null"/> in that case.
    /// </summary>
    public static XInterceptStatistics CalculateXIntercept(LinearRegressionResult regression, double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(regression);
        ValidateAlpha(alpha);

        var slope = regression.Line.Slope;
        var intercept = regression.Line.Intercept;
        var value = -intercept / slope;

        var tCritical = StudentTDistributionCalculator.TwoTailedCriticalValue(alpha, regression.Significance.DegreesOfFreedom);
        var meanX = regression.Input.MeanX;
        var sumSquaredXDeviations = regression.Input.SumSquaredXDeviations;
        var tSquaredSSquared = Square(tCritical) * Square(regression.Fit.ResidualStandardError);
        var g = tSquaredSSquared / (Square(slope) * sumSquaredXDeviations);

        if (!(g < 1))
        {
            return new XInterceptStatistics(value, null);
        }

        var a = Square(slope) * (1 - g);
        var b = 2 * slope * (intercept + (g * slope * meanX));
        var c = Square(intercept) - (tSquaredSSquared / regression.Input.Count) - (g * Square(slope) * Square(meanX));

        var discriminant = Square(b) - (4 * a * c);
        if (discriminant < 0)
        {
            return new XInterceptStatistics(value, null);
        }

        var sqrtDiscriminant = Math.Sqrt(discriminant);
        var root1 = (-b - sqrtDiscriminant) / (2 * a);
        var root2 = (-b + sqrtDiscriminant) / (2 * a);

        return new XInterceptStatistics(value, new ConfidenceInterval(Math.Min(root1, root2), Math.Max(root1, root2)));
    }

    private static DataPoint[] ValidatePoints(IEnumerable<DataPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var observations = points.ToArray();

        if (observations.Length < MinimumPointCount)
        {
            throw new ArgumentException(
                $"At least {MinimumPointCount} data points are required for regression inference.",
                nameof(points));
        }

        foreach (var observation in observations)
        {
            ValidateFinite(observation.X, nameof(points));
            ValidateFinite(observation.Y, nameof(points));
        }

        return observations;
    }

    private static RegressionInputSummary CalculateInputSummary(DataPoint[] observations)
    {
        var count = observations.Length;
        var meanX = observations.Average(point => point.X);
        var meanY = observations.Average(point => point.Y);
        var sumSquaredXDeviations = observations.Sum(point => Square(point.X - meanX));
        var sumSquaredYDeviations = observations.Sum(point => Square(point.Y - meanY));

        if (sumSquaredXDeviations == 0)
        {
            throw new ArgumentException("Regression requires at least two distinct X values.");
        }

        return new RegressionInputSummary(
            count,
            observations.Min(point => point.X),
            observations.Max(point => point.X),
            meanX,
            meanY,
            sumSquaredXDeviations,
            sumSquaredYDeviations,
            observations.Select(point => point.X).Distinct().Count() != count);
    }

    private static RegressionLine CalculateBestFitLine(
        DataPoint[] observations,
        RegressionInputSummary input)
    {
        var sumProductDeviations = observations.Sum(
            point => (point.X - input.MeanX) * (point.Y - input.MeanY));
        var slope = sumProductDeviations / input.SumSquaredXDeviations;
        var intercept = input.MeanY - (slope * input.MeanX);

        return new RegressionLine(slope, intercept);
    }

    private static RegressionFit CalculateFit(
        DataPoint[] observations,
        RegressionInputSummary input,
        RegressionLine line)
    {
        var residualSumOfSquares = observations.Sum(
            point => Square(point.Y - line.Predict(point.X)));
        residualSumOfSquares = CleanNearZero(residualSumOfSquares);

        var regressionSumOfSquares = CleanNearZero(input.SumSquaredYDeviations - residualSumOfSquares);
        var rSquared = input.SumSquaredYDeviations == 0
            ? double.NaN
            : Clamp(1 - (residualSumOfSquares / input.SumSquaredYDeviations), 0, 1);
        var residualStandardError = Math.Sqrt(residualSumOfSquares / DegreesOfFreedom(input.Count));

        return new RegressionFit(
            rSquared,
            residualStandardError,
            residualSumOfSquares,
            input.SumSquaredYDeviations,
            regressionSumOfSquares);
    }

    private static RegressionSignificance CalculateSignificance(
        RegressionInputSummary input,
        RegressionLine line,
        RegressionFit fit,
        double alpha)
    {
        var degreesOfFreedom = DegreesOfFreedom(input.Count);
        var meanSquaredError = fit.ResidualSumOfSquares / degreesOfFreedom;
        var slopeStandardError = Math.Sqrt(meanSquaredError / input.SumSquaredXDeviations);
        var tStatistic = CalculateTStatistic(line.Slope, slopeStandardError);
        var fStatistic = double.IsInfinity(tStatistic)
            ? double.PositiveInfinity
            : tStatistic * tStatistic;
        var pValue = CalculateSlopePValue(tStatistic, degreesOfFreedom);
        var slopeConfidenceInterval = CalculateSlopeConfidenceInterval(
            line.Slope,
            slopeStandardError,
            degreesOfFreedom,
            alpha);

        return new RegressionSignificance(
            slopeStandardError,
            tStatistic,
            fStatistic,
            pValue,
            degreesOfFreedom,
            alpha,
            slopeConfidenceInterval,
            pValue < alpha);
    }

    private static ConfidenceInterval CalculateSlopeConfidenceInterval(
        double slope,
        double slopeStandardError,
        int degreesOfFreedom,
        double alpha)
    {
        if (slopeStandardError == 0)
        {
            return new ConfidenceInterval(slope, slope);
        }

        var tCritical = StudentTDistributionCalculator.TwoTailedCriticalValue(alpha, degreesOfFreedom);
        var margin = tCritical * slopeStandardError;

        return new ConfidenceInterval(slope - margin, slope + margin);
    }

    private static int DegreesOfFreedom(int count) => count - 2;

    private static double CalculateTStatistic(double slope, double slopeStandardError)
    {
        if (slopeStandardError != 0)
        {
            return slope / slopeStandardError;
        }

        if (slope == 0)
        {
            return 0;
        }

        return slope > 0 ? double.PositiveInfinity : double.NegativeInfinity;
    }

    private static double CalculateSlopePValue(double tStatistic, int degreesOfFreedom)
    {
        if (double.IsInfinity(tStatistic))
        {
            return 0;
        }

        return StudentTDistributionCalculator.TwoTailedPValue(tStatistic, degreesOfFreedom);
    }

    private static void ValidateAlpha(double alpha)
    {
        if (double.IsNaN(alpha) || alpha <= 0 || alpha >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be greater than 0 and less than 1.");
        }
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("Regression values must be finite numbers.", parameterName);
        }
    }

    private static double CleanNearZero(double value) => Math.Abs(value) < 1e-24 ? 0 : value;

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }
}
