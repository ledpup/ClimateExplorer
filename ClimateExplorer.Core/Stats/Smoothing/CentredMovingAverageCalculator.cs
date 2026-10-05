namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Moving average whose window is centred on each point (for an even window size, by giving the two
/// end slots half weight). Points whose window would run off either end of the series are <c>null</c>.
/// </summary>
public sealed class CentredMovingAverageCalculator : ISeriesSmoother
{
    public SmoothedSeries Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold)
    {
        var result = new double?[values.Count];
        var windowSizes = new int[values.Count];
        var windowStarts = new int[values.Count];
        var windowEnds = new int[values.Count];

        int reach = windowSize / 2;

        for (int i = reach; i < values.Count - reach; i++)
        {
            (result[i], windowSizes[i], windowStarts[i], windowEnds[i]) = SmoothingWindow.CentredMean(values, i, windowSize, 0, values.Count - 1, requiredDataThreshold);
        }

        return new SmoothedSeries(result, windowSizes, windowStarts, windowEnds);
    }
}
