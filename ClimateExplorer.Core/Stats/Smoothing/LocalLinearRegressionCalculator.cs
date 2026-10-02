namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Local linear regression with a uniform window: fits a straight line (ordinary least squares, every
/// value in the window weighted equally) and takes the line's value at the point being smoothed.
/// See https://en.wikipedia.org/wiki/Local_regression.
/// </summary>
/// <remarks>
/// Gives a value from the first year of data to the last (see <see cref="BoundaryAdjustedWindow"/>).
/// Near either end the point sits off-centre in its window, so the line carries the local trend out
/// to it. Where the window is symmetric about the point and has no gaps, the result equals the mean
/// of the window, i.e. the centred moving average.
/// </remarks>
public sealed class LocalLinearRegressionCalculator : BoundaryAdjustedWindowCalculator
{
    protected override double? Estimate(IReadOnlyList<double?> values, int windowStart, int windowSize, int targetIndex)
    {
        // x is measured from the target, so the fitted line's intercept is its value at the target.
        int count = 0;
        double sumX = 0;
        double sumY = 0;
        double sumXX = 0;
        double sumXY = 0;

        for (int i = windowStart; i < windowStart + windowSize; i++)
        {
            if (values[i] is double y)
            {
                double x = i - targetIndex;

                count++;
                sumX += x;
                sumY += y;
                sumXX += x * x;
                sumXY += x * y;
            }
        }

        if (count == 0)
        {
            return null;
        }

        double denominator = (count * sumXX) - (sumX * sumX);

        // A single value can't define a slope, so the line is flat through it.
        if (denominator == 0)
        {
            return sumY / count;
        }

        return ((sumY * sumXX) - (sumX * sumXY)) / denominator;
    }
}
