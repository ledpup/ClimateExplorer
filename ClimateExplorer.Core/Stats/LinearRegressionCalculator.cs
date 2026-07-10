namespace ClimateExplorer.Core.Stats;

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
            + (Math.Pow(x - regression.Input.MeanX, 2) / regression.Input.SumSquaredXDeviations);

        var tCritical = StudentTDistribution.TwoTailedCriticalValue(
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
        var sumSquaredXDeviations = observations.Sum(point => Math.Pow(point.X - meanX, 2));
        var sumSquaredYDeviations = observations.Sum(point => Math.Pow(point.Y - meanY, 2));

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
            point => Math.Pow(point.Y - line.Predict(point.X), 2));
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

        var tCritical = StudentTDistribution.TwoTailedCriticalValue(alpha, degreesOfFreedom);
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

        return StudentTDistribution.TwoTailedPValue(tStatistic, degreesOfFreedom);
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
