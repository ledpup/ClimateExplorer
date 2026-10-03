namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Smooths a series of evenly spaced values. The output always has the same length as the input.
/// </summary>
public interface ISeriesSmoother
{
    /// <summary>
    /// Smooths <paramref name="values"/>, one value per bin, where missing bins are <c>null</c>.
    /// A point gets a smoothed value only if at least <paramref name="requiredDataThreshold"/> of the
    /// <paramref name="windowSize"/> slots in its window hold a value. Otherwise the point is <c>null</c>.
    /// The result also gives the size of the window each value was averaged over.
    /// </summary>
    SmoothedSeries Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold);
}
