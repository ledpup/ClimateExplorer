namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Shared loop for smoothers that use a <see cref="BoundaryAdjustedWindow"/>. The data range runs from
/// the first to the last non-null value. Points outside it are always <c>null</c>, and so is every point
/// if the data range is shorter than the window. Derived classes supply only the estimate for a window.
/// </summary>
public abstract class BoundaryAdjustedWindowCalculator : ISeriesSmoother
{
    public double?[] Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(windowSize, 1);

        if (requiredDataThreshold <= 0 || requiredDataThreshold > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredDataThreshold), requiredDataThreshold, "Must be greater than 0 and no more than 1.");
        }

        var result = new double?[values.Count];

        int firstIndex = FindFirstIndexWithValue(values);
        int lastIndex = FindLastIndexWithValue(values);

        if (firstIndex < 0 || lastIndex - firstIndex + 1 < windowSize)
        {
            return result;
        }

        for (int i = firstIndex; i <= lastIndex; i++)
        {
            int windowStart = BoundaryAdjustedWindow.GetStart(i, windowSize, firstIndex, lastIndex);

            if (SmoothingWindow.MeetsThreshold(values, windowStart, windowSize, requiredDataThreshold))
            {
                result[i] = Estimate(values, windowStart, windowSize, i);
            }
        }

        return result;
    }

    /// <summary>
    /// The smoothed value for the point at <paramref name="targetIndex"/>, from
    /// <c>values[windowStart .. windowStart + windowSize - 1]</c>. Only called when the window meets the
    /// threshold. Returns <c>null</c> if no estimate is possible.
    /// </summary>
    protected abstract double? Estimate(IReadOnlyList<double?> values, int windowStart, int windowSize, int targetIndex);

    private static int FindFirstIndexWithValue(IReadOnlyList<double?> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].HasValue)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindLastIndexWithValue(IReadOnlyList<double?> values)
    {
        for (int i = values.Count - 1; i >= 0; i--)
        {
            if (values[i].HasValue)
            {
                return i;
            }
        }

        return -1;
    }
}
