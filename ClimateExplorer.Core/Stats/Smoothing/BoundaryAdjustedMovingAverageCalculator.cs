namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Moving average that gives a value from the first year of data to the last. The window is centred in
/// the middle of the data, looks forward at the start and trails at the end (see <see cref="BoundaryAdjustedWindow"/>).
/// </summary>
public sealed class BoundaryAdjustedMovingAverageCalculator : BoundaryAdjustedWindowCalculator
{
    protected override double? Estimate(IReadOnlyList<double?> values, int windowStart, int windowSize, int targetIndex)
    {
        return SmoothingWindow.Mean(values, windowStart, windowSize);
    }
}
