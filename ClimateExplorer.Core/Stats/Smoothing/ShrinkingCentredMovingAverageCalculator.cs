namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Moving average whose window is centred on each point (for an even window size, with the extra
/// slot before the point). Near either end of the data the window shrinks on both sides so it stays
/// centred, until its half-width reaches <see cref="MinimumHalfWindow"/>. Past that, only the side
/// facing the end of the data is cut off. The data runs from the first to the last non-null value.
/// </summary>
public sealed class ShrinkingCentredMovingAverageCalculator : ISeriesSmoother
{
    private const int MinimumHalfWindow = 4;

    public SmoothedSeries Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold)
    {
        var result = new double?[values.Count];
        var windowSizes = new int[values.Count];

        int first = 0;
        while (first < values.Count && !values[first].HasValue)
        {
            first++;
        }

        int last = values.Count - 1;
        while (last > first && !values[last].HasValue)
        {
            last--;
        }

        int before = windowSize / 2;
        int after = windowSize - 1 - before;
        int extra = before - after;
        int floor = Math.Min(MinimumHalfWindow, after);

        for (int i = first; i <= last; i++)
        {
            int room = Math.Min(i - first, last - i);
            int reach = Math.Min(after, Math.Max(room, floor));

            int windowStart = Math.Max(first, i - reach - extra);
            int windowEnd = Math.Min(last, i + reach);
            int slotsInWindow = windowEnd - windowStart + 1;

            if (SmoothingWindow.MeetsThreshold(values, windowStart, slotsInWindow, requiredDataThreshold))
            {
                result[i] = SmoothingWindow.Mean(values, windowStart, slotsInWindow);
                windowSizes[i] = result[i].HasValue ? slotsInWindow : 0;
            }
        }

        return new SmoothedSeries(result, windowSizes);
    }
}
