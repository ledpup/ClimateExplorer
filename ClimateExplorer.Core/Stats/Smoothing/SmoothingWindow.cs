namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// Calculations on one window of a series, shared by the smoothers so they all treat gaps the same way.
/// </summary>
internal static class SmoothingWindow
{
    /// <summary>
    /// Mean of the window of <paramref name="windowSize"/> centred on <paramref name="centre"/>, and
    /// the size of that window. An even window can't be centred on a point, so it is a 2xN moving
    /// average: N + 1 slots with the two end slots at half weight (e.g. 0.5, 1, 1, 1, 0.5 for N = 4).
    /// Slots outside <paramref name="first"/> to <paramref name="last"/> are cut off the window.
    /// The mean is <c>null</c>, and the size 0, if less than <paramref name="requiredDataThreshold"/>
    /// of the window's weight holds a value.
    /// </summary>
    public static (double? Mean, int Size) CentredMean(IReadOnlyList<double?> values, int centre, int windowSize, int first, int last, float requiredDataThreshold)
    {
        int reach = windowSize / 2;
        bool isEvenWindow = windowSize % 2 == 0;

        double sum = 0;
        double weightPresent = 0;
        double weightOfWindow = 0;

        for (int i = Math.Max(first, centre - reach); i <= Math.Min(last, centre + reach); i++)
        {
            double weight = isEvenWindow && Math.Abs(i - centre) == reach ? 0.5 : 1;

            weightOfWindow += weight;

            if (values[i] is double value)
            {
                sum += value * weight;
                weightPresent += weight;
            }
        }

        if (weightPresent > 0 && (float)(weightPresent / weightOfWindow) >= requiredDataThreshold)
        {
            return (sum / weightPresent, (int)weightOfWindow);
        }

        return (null, 0);
    }
}
