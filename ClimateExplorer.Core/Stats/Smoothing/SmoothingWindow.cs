namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Calculations on one window of a series, shared by the smoothers so they all treat gaps the same way.
/// A window is <c>values[windowStart .. windowStart + windowSize - 1]</c>.
/// </summary>
internal static class SmoothingWindow
{
    /// <summary>
    /// True if at least <paramref name="requiredDataThreshold"/> of the window's slots hold a value.
    /// </summary>
    public static bool MeetsThreshold(IReadOnlyList<double?> values, int windowStart, int windowSize, float requiredDataThreshold)
    {
        int present = 0;

        for (int i = windowStart; i < windowStart + windowSize; i++)
        {
            if (values[i].HasValue)
            {
                present++;
            }
        }

        return present / (float)windowSize >= requiredDataThreshold;
    }

    /// <summary>
    /// Mean of the non-null values in the window, or <c>null</c> if there are none.
    /// </summary>
    public static double? Mean(IReadOnlyList<double?> values, int windowStart, int windowSize)
    {
        double sum = 0;
        int count = 0;

        for (int i = windowStart; i < windowStart + windowSize; i++)
        {
            if (values[i] is double value)
            {
                sum += value;
                count++;
            }
        }

        return count == 0 ? null : sum / count;
    }
}
