namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Places a fixed-width window for each point. In the middle of the data the window is centred on the
/// point (for an even window size, with the extra slot before the point). Near either end it is moved
/// inward until it fits inside the data, so it looks forward at the start and trails at the end.
/// </summary>
public static class BoundaryAdjustedWindow
{
    /// <summary>
    /// Index of the first slot in the window for the point at <paramref name="index"/>, where the data
    /// runs from <paramref name="firstIndex"/> to <paramref name="lastIndex"/> inclusive. The data must
    /// be at least <paramref name="windowSize"/> long.
    /// </summary>
    public static int GetStart(int index, int windowSize, int firstIndex, int lastIndex)
    {
        int centredStart = index - (windowSize / 2);
        int lastPossibleStart = lastIndex - windowSize + 1;

        return Math.Clamp(centredStart, firstIndex, lastPossibleStart);
    }
}
