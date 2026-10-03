namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Moving average whose window is centred on each point (for an even window size, by giving the two
/// end slots half weight). Near either end of the data the window shrinks on both sides, to an odd
/// size so it stays centred, until its half-width reaches <see cref="MinimumHalfWindow"/>. Past that,
/// only the side facing the end of the data is cut off. The data runs from the first to the last
/// non-null value.
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

        int halfWindow = windowSize / 2;
        int floor = Math.Min(MinimumHalfWindow, halfWindow);

        for (int i = first; i <= last; i++)
        {
            int room = Math.Min(i - first, last - i);
            int reach = Math.Min(halfWindow, Math.Max(room, floor));
            int shrunkWindowSize = Math.Min(windowSize, (2 * reach) + 1);

            (result[i], windowSizes[i]) = SmoothingWindow.CentredMean(values, i, shrunkWindowSize, first, last, requiredDataThreshold);
        }

        return new SmoothedSeries(result, windowSizes);
    }
}
