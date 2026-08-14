namespace ClimateExplorer.Core.Stats;

using ClimateExplorer.Core.Stats.Model;
using static MathHelpers;

/// <summary>
/// Ordinary least squares regression of Y against a polynomial in X, degree 1 (a straight line, the
/// original <c>LinearRegressionCalculator</c> behaviour, unchanged) through 3 (cubic). Degrees above
/// 3 are a deliberate non-goal - <see cref="MaximumDegree"/> - not just unimplemented, since a
/// higher-degree fit needs materially more data to mean anything and this codebase has no use for
/// one yet.
/// </summary>
/// <remarks>
/// Fits internally in coordinates centred on the data's mean X (<c>x' = x - meanX</c>) for numerical
/// stability - the normal-equations matrix for a cubic fit in raw calendar-year units would need
/// sums of X^6 (calendar years to the 6th power), which loses meaningful precision in a double long
/// before it loses correctness in centred units, where X' is a few dozen at most. The fitted
/// coefficients are shifted back to the original X scale before being returned (see
/// <see cref="PolynomialCurve.Coefficients"/>), via an exact linear change of basis (binomial
/// expansion of <c>(x - meanX)^k</c>) - not an approximation - so <c>Predict</c> is a plain
/// evaluation at the real X (a calendar year like 2100) with no separate "which year was this
/// centred on" bookkeeping needed by the caller, and a degree-1 fit reproduces the pre-polynomial
/// closed-form slope/intercept exactly.
/// </remarks>
public static class PolynomialRegressionCalculator
{
    public const int MinimumDegree = 1;
    public const int MaximumDegree = 3;

    public static PolynomialRegressionResult Calculate(IEnumerable<DataPoint> points, int degree = 1, double alpha = 0.05)
    {
        ValidateDegree(degree);
        ValidateAlpha(alpha);

        var observations = ValidatePoints(points, degree);
        var input = CalculateInputSummary(observations);
        ValidateDistinctXCount(observations, degree);

        var termCount = degree + 1;
        var normalMatrix = BuildNormalMatrix(observations, input.MeanX, degree);
        var rhs = BuildWeightedPowerSums(observations, input.MeanX, degree);
        var normalMatrixInverse = InvertMatrix(normalMatrix, termCount);
        var centredCoefficients = MultiplyMatrixVector(normalMatrixInverse, rhs, termCount);

        var residualSumOfSquares = CleanNearZero(CalculateResidualSumOfSquares(observations, input.MeanX, centredCoefficients));
        var regressionSumOfSquares = CleanNearZero(input.SumSquaredYDeviations - residualSumOfSquares);
        var residualDegreesOfFreedom = input.Count - termCount;
        var residualVariance = residualSumOfSquares / residualDegreesOfFreedom;
        var residualStandardError = Math.Sqrt(residualVariance);
        var rSquared = input.SumSquaredYDeviations == 0
            ? double.NaN
            : Clamp(1 - (residualSumOfSquares / input.SumSquaredYDeviations), 0, 1);

        var fit = new RegressionFit(rSquared, residualStandardError, residualSumOfSquares, input.SumSquaredYDeviations, regressionSumOfSquares);

        // Shift from centred (x - meanX) coefficients to the original X scale - see remarks.
        var shift = BuildShiftMatrix(input.MeanX, termCount);
        var coefficients = MultiplyMatrixVector(shift, centredCoefficients, termCount);
        var curve = new PolynomialCurve(coefficients);

        var centredCovariance = ScaleMatrix(normalMatrixInverse, residualVariance, termCount);
        var covariance = PropagateCovariance(shift, centredCovariance, termCount);

        var significance = CalculateSignificance(degree, residualDegreesOfFreedom, regressionSumOfSquares, residualVariance, coefficients, covariance, alpha);

        return new PolynomialRegressionResult(input, degree, curve, fit, significance, ToJaggedArray(covariance, termCount));
    }

    public static RegressionPrediction Predict(
        PolynomialRegressionResult regression,
        double x,
        double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(regression);
        ValidateAlpha(alpha);
        ValidateFinite(x, nameof(x));

        var predictedY = regression.Curve.Predict(x);
        var weights = PowersOf(x, regression.Degree);
        var fittedMeanVariance = Math.Max(0, QuadraticForm(regression.CoefficientCovarianceMatrix, weights));

        var tCritical = StudentTDistributionCalculator.TwoTailedCriticalValue(
            alpha,
            regression.Significance.DegreesOfFreedom);

        var meanMargin = tCritical * Math.Sqrt(fittedMeanVariance);
        var observationMargin = tCritical * Math.Sqrt(fittedMeanVariance + Square(regression.Fit.ResidualStandardError));

        return new RegressionPrediction(
            x,
            predictedY,
            new ConfidenceInterval(predictedY - meanMargin, predictedY + meanMargin),
            new ConfidenceInterval(predictedY - observationMargin, predictedY + observationMargin),
            alpha);
    }

    /// <summary>
    /// The Y-intercept is the fitted mean at X = 0, so its standard error and confidence interval
    /// are exactly what <see cref="Predict"/> already computes at that point, for any degree.
    /// </summary>
    public static InterceptStatistics CalculateInterceptStatistics(PolynomialRegressionResult regression, double alpha = 0.05)
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
    /// The X-intercept is the X where the fitted line crosses Y = 0, found via Fieller's theorem
    /// (Fieller, 1954; Draper &amp; Smith, <i>Applied Regression Analysis</i>, S5.3) - see the
    /// original linear-regression design doc for the derivation. This is specifically a two-
    /// coefficient (slope/intercept) method: it doesn't generalise to a curve with more than one
    /// non-constant term, so it's only offered for a degree-1 fit. For a quadratic/cubic fit, find
    /// where the curve crosses zero with <see cref="PolynomialRootFinder"/> instead - a different,
    /// simpler question ("where are the real roots") with no confidence interval attached, since
    /// Fieller's argument doesn't extend to more than one correlated coefficient.
    /// </summary>
    public static XInterceptStatistics CalculateXIntercept(PolynomialRegressionResult regression, double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(regression);
        ValidateAlpha(alpha);

        if (regression.Degree != 1)
        {
            throw new NotSupportedException(
                "X-intercept via Fieller's theorem is only defined for a linear (degree 1) fit. "
                + "Use PolynomialRootFinder to find point-estimate real roots of a quadratic/cubic fit instead.");
        }

        var slope = regression.Curve.Slope;
        var intercept = regression.Curve.Intercept;
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

    /// <summary>
    /// The curve's instantaneous rate of change at <paramref name="atX"/> (<see cref="PolynomialCurve.Derivative"/>),
    /// with its own standard error and confidence interval - the properly-propagated uncertainty
    /// behind that single number, for anywhere "the rate" needs a ± alongside it rather than just the
    /// point value. At degree 1 this reproduces <see cref="RegressionSignificance.SlopeStandardError"/>
    /// and <see cref="RegressionSignificance.SlopeConfidenceInterval"/> exactly, at any X - a line's
    /// derivative is the same constant everywhere. At degree 2/3 the rate is a linear combination of
    /// every non-constant coefficient (<c>d/dx = c1 + 2·c2·x + 3·c3·x²</c>), so its variance is the
    /// same quadratic-form calculation <see cref="Predict"/> uses for the fitted value, with a
    /// different weight vector - one that omits the constant term entirely and weights each other
    /// term by its power.
    /// </summary>
    public static RateOfChangeEstimate CalculateRateOfChange(PolynomialRegressionResult regression, double atX, double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(regression);
        ValidateAlpha(alpha);
        ValidateFinite(atX, nameof(atX));

        var rate = regression.Curve.Derivative(atX);
        var weights = DerivativeWeights(atX, regression.Degree);
        var variance = Math.Max(0, QuadraticForm(regression.CoefficientCovarianceMatrix, weights));
        var standardError = Math.Sqrt(variance);

        var tCritical = StudentTDistributionCalculator.TwoTailedCriticalValue(alpha, regression.Significance.DegreesOfFreedom);
        var margin = tCritical * standardError;

        return new RateOfChangeEstimate(atX, rate, standardError, new ConfidenceInterval(rate - margin, rate + margin));
    }

    /// <summary>
    /// The standard error, t-statistic and confidence interval for every fitted coefficient - the
    /// per-term breakdown a single "Slope"/"Y-intercept" pair can't give once a fit has more than two
    /// terms. At degree 1 this returns exactly the same two numbers <see cref="CalculateInterceptStatistics"/>
    /// and <see cref="RegressionSignificance.SlopeStandardError"/> already carry.
    /// </summary>
    public static IReadOnlyList<CoefficientStatistics> CalculateCoefficientStatistics(PolynomialRegressionResult regression, double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(regression);
        ValidateAlpha(alpha);

        var tCritical = StudentTDistributionCalculator.TwoTailedCriticalValue(alpha, regression.Significance.DegreesOfFreedom);
        var results = new List<CoefficientStatistics>();

        for (var power = 0; power <= regression.Degree; power++)
        {
            var value = regression.Curve.Coefficients[power];
            var standardError = Math.Sqrt(Math.Max(0, regression.CoefficientCovarianceMatrix[power][power]));
            var tStatistic = CalculateTStatistic(value, standardError);
            var margin = tCritical * standardError;

            results.Add(new CoefficientStatistics(power, value, standardError, tStatistic, new ConfidenceInterval(value - margin, value + margin)));
        }

        return results;
    }

    private static DataPoint[] ValidatePoints(IEnumerable<DataPoint> points, int degree)
    {
        ArgumentNullException.ThrowIfNull(points);

        var observations = points.ToArray();
        var minimumPointCount = degree + 2;

        if (observations.Length < minimumPointCount)
        {
            throw new ArgumentException(
                $"At least {minimumPointCount} data points are required for a degree-{degree} regression's inference.",
                nameof(points));
        }

        foreach (var observation in observations)
        {
            ValidateFinite(observation.X, nameof(points));
            ValidateFinite(observation.Y, nameof(points));
        }

        return observations;
    }

    private static void ValidateDistinctXCount(DataPoint[] observations, int degree)
    {
        var distinctXCount = observations.Select(point => point.X).Distinct().Count();

        if (distinctXCount <= degree)
        {
            throw new ArgumentException(
                $"A degree-{degree} regression requires at least {degree + 1} distinct X values.",
                nameof(observations));
        }
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

    /// <summary>
    /// The (degree+1)x(degree+1) normal-equations matrix <c>N</c> in centred coordinates, where
    /// <c>N[i,j] = Σ(x - meanX)^(i+j)</c> - built from a single pass of centred power sums since
    /// every entry is one of at most <c>2·degree + 1</c> distinct power sums.
    /// </summary>
    private static double[,] BuildNormalMatrix(DataPoint[] observations, double meanX, int degree)
    {
        var powerSums = CalculateCentredPowerSums(observations, meanX, 2 * degree);
        var termCount = degree + 1;
        var matrix = new double[termCount, termCount];

        for (var i = 0; i < termCount; i++)
        {
            for (var j = 0; j < termCount; j++)
            {
                matrix[i, j] = powerSums[i + j];
            }
        }

        return matrix;
    }

    private static double[] BuildWeightedPowerSums(DataPoint[] observations, double meanX, int degree)
    {
        var termCount = degree + 1;
        var weightedPowerSums = new double[termCount];

        foreach (var observation in observations)
        {
            var centredX = observation.X - meanX;
            var power = 1.0;

            for (var i = 0; i < termCount; i++)
            {
                weightedPowerSums[i] += power * observation.Y;
                power *= centredX;
            }
        }

        return weightedPowerSums;
    }

    private static double[] CalculateCentredPowerSums(DataPoint[] observations, double meanX, int maxPower)
    {
        var powerSums = new double[maxPower + 1];

        foreach (var observation in observations)
        {
            var centredX = observation.X - meanX;
            var power = 1.0;

            for (var k = 0; k <= maxPower; k++)
            {
                powerSums[k] += power;
                power *= centredX;
            }
        }

        return powerSums;
    }

    private static double CalculateResidualSumOfSquares(DataPoint[] observations, double meanX, double[] centredCoefficients)
    {
        var sum = 0.0;

        foreach (var observation in observations)
        {
            var predicted = EvaluatePolynomial(centredCoefficients, observation.X - meanX);
            sum += Square(observation.Y - predicted);
        }

        return sum;
    }

    /// <summary>
    /// The exact linear map from centred coefficients <c>a</c> (of <c>x - meanX</c>) to original-
    /// scale coefficients <c>c</c> (of <c>x</c>): expanding <c>(x - meanX)^k</c> via the binomial
    /// theorem gives <c>c_j = Σ(k ≥ j) a_k · C(k,j) · (-meanX)^(k-j)</c>, i.e. row <c>j</c>, column
    /// <c>k</c> of this matrix is <c>C(k,j) · (-meanX)^(k-j)</c> for <c>k ≥ j</c> and 0 otherwise.
    /// A change of polynomial basis, not an approximation - applying it to the fitted coefficients'
    /// covariance matrix too (<c>T·Cov·Tᵀ</c>) is exact for the same reason.
    /// </summary>
    private static double[,] BuildShiftMatrix(double meanX, int termCount)
    {
        var shift = new double[termCount, termCount];

        for (var j = 0; j < termCount; j++)
        {
            for (var k = j; k < termCount; k++)
            {
                shift[j, k] = BinomialCoefficient(k, j) * Math.Pow(-meanX, k - j);
            }
        }

        return shift;
    }

    private static double BinomialCoefficient(int n, int k)
    {
        if (k < 0 || k > n)
        {
            return 0;
        }

        var result = 1.0;

        for (var i = 0; i < k; i++)
        {
            result = result * (n - i) / (i + 1);
        }

        return result;
    }

    private static double[,] PropagateCovariance(double[,] shift, double[,] centredCovariance, int termCount)
    {
        var shifted = MultiplyMatrices(shift, centredCovariance, termCount);
        var shiftTranspose = TransposeMatrix(shift, termCount);

        return MultiplyMatrices(shifted, shiftTranspose, termCount);
    }

    private static RegressionSignificance CalculateSignificance(
        int degree,
        int residualDegreesOfFreedom,
        double regressionSumOfSquares,
        double residualVariance,
        double[] coefficients,
        double[,] covariance,
        double alpha)
    {
        var slope = coefficients[1];
        var slopeStandardError = Math.Sqrt(Math.Max(0, covariance[1, 1]));
        var tStatistic = CalculateTStatistic(slope, slopeStandardError);

        var fStatistic = residualVariance == 0
            ? double.PositiveInfinity
            : (regressionSumOfSquares / degree) / residualVariance;
        var pValue = double.IsPositiveInfinity(fStatistic)
            ? 0
            : FDistributionCalculator.UpperTailPValue(fStatistic, degree, residualDegreesOfFreedom);

        var slopeConfidenceInterval = CalculateSlopeConfidenceInterval(slope, slopeStandardError, residualDegreesOfFreedom, alpha);

        return new RegressionSignificance(
            slopeStandardError,
            tStatistic,
            fStatistic,
            pValue,
            residualDegreesOfFreedom,
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

    private static double CalculateTStatistic(double value, double standardError)
    {
        if (standardError != 0)
        {
            return value / standardError;
        }

        if (value == 0)
        {
            return 0;
        }

        return value > 0 ? double.PositiveInfinity : double.NegativeInfinity;
    }

    private static void ValidateDegree(int degree)
    {
        if (degree < MinimumDegree || degree > MaximumDegree)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degree),
                $"Degree must be between {MinimumDegree} and {MaximumDegree}.");
        }
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

    private static double EvaluatePolynomial(IReadOnlyList<double> coefficients, double x)
    {
        var result = 0.0;

        for (var i = coefficients.Count - 1; i >= 0; i--)
        {
            result = (result * x) + coefficients[i];
        }

        return result;
    }

    private static double[] DerivativeWeights(double x, int degree)
    {
        var weights = new double[degree + 1];
        var power = 1.0;

        for (var k = 1; k <= degree; k++)
        {
            weights[k] = k * power;
            power *= x;
        }

        return weights;
    }

    private static double[] PowersOf(double x, int degree)
    {
        var powers = new double[degree + 1];
        var power = 1.0;

        for (var i = 0; i <= degree; i++)
        {
            powers[i] = power;
            power *= x;
        }

        return powers;
    }

    private static double QuadraticForm(IReadOnlyList<IReadOnlyList<double>> matrix, double[] weights)
    {
        var sum = 0.0;

        for (var i = 0; i < weights.Length; i++)
        {
            for (var j = 0; j < weights.Length; j++)
            {
                sum += matrix[i][j] * weights[i] * weights[j];
            }
        }

        return sum;
    }

    /// <summary>Gauss-Jordan elimination with partial pivoting, augmented with the identity matrix.</summary>
    private static double[,] InvertMatrix(double[,] matrix, int size)
    {
        var augmented = new double[size, size * 2];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                augmented[i, j] = matrix[i, j];
            }

            augmented[i, size + i] = 1;
        }

        for (var col = 0; col < size; col++)
        {
            var pivotRow = col;
            var maxAbsValue = Math.Abs(augmented[col, col]);

            for (var row = col + 1; row < size; row++)
            {
                if (Math.Abs(augmented[row, col]) > maxAbsValue)
                {
                    maxAbsValue = Math.Abs(augmented[row, col]);
                    pivotRow = row;
                }
            }

            if (pivotRow != col)
            {
                for (var j = 0; j < size * 2; j++)
                {
                    (augmented[col, j], augmented[pivotRow, j]) = (augmented[pivotRow, j], augmented[col, j]);
                }
            }

            var pivotValue = augmented[col, col];

            if (pivotValue == 0)
            {
                throw new ArgumentException("The fitted points do not uniquely determine a regression of this degree (collinear/insufficiently varied X values).");
            }

            for (var j = 0; j < size * 2; j++)
            {
                augmented[col, j] /= pivotValue;
            }

            for (var row = 0; row < size; row++)
            {
                if (row == col)
                {
                    continue;
                }

                var factor = augmented[row, col];

                if (factor == 0)
                {
                    continue;
                }

                for (var j = 0; j < size * 2; j++)
                {
                    augmented[row, j] -= factor * augmented[col, j];
                }
            }
        }

        var inverse = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                inverse[i, j] = augmented[i, size + j];
            }
        }

        return inverse;
    }

    private static double[] MultiplyMatrixVector(double[,] matrix, double[] vector, int size)
    {
        var result = new double[size];

        for (var i = 0; i < size; i++)
        {
            var sum = 0.0;

            for (var j = 0; j < size; j++)
            {
                sum += matrix[i, j] * vector[j];
            }

            result[i] = sum;
        }

        return result;
    }

    private static double[,] MultiplyMatrices(double[,] left, double[,] right, int size)
    {
        var result = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                var sum = 0.0;

                for (var k = 0; k < size; k++)
                {
                    sum += left[i, k] * right[k, j];
                }

                result[i, j] = sum;
            }
        }

        return result;
    }

    private static double[,] TransposeMatrix(double[,] matrix, int size)
    {
        var result = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                result[j, i] = matrix[i, j];
            }
        }

        return result;
    }

    private static double[,] ScaleMatrix(double[,] matrix, double scalar, int size)
    {
        var result = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                result[i, j] = matrix[i, j] * scalar;
            }
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyList<double>> ToJaggedArray(double[,] matrix, int size)
    {
        var rows = new double[size][];

        for (var i = 0; i < size; i++)
        {
            var row = new double[size];

            for (var j = 0; j < size; j++)
            {
                row[j] = matrix[i, j];
            }

            rows[i] = row;
        }

        return rows;
    }
}
