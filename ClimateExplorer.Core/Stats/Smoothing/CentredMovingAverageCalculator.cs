namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Moving average whose window is centred on each point (for an even window size, with the extra
/// slot before the point). Points whose window would run off either end of the series are <c>null</c>.
/// </summary>
public sealed class CentredMovingAverageCalculator : ISeriesSmoother
{
    public double?[] Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold)
    {
        var result = new double?[values.Count];

        for (int i = 0; i < values.Count; i++)
        {
            int windowStart = i - (windowSize / 2);
            int windowEnd = windowStart + windowSize - 1;

            if (windowStart >= 0
                && windowEnd < values.Count
                && SmoothingWindow.MeetsThreshold(values, windowStart, windowSize, requiredDataThreshold))
            {
                result[i] = SmoothingWindow.Mean(values, windowStart, windowSize);
            }
        }

        return result;
    }
}
