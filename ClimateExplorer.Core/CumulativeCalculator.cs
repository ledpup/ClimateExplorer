namespace ClimateExplorer.Core;

public static class CumulativeCalculator
{
    /// <summary>
    /// Accumulates the measured value over time, running total. A null value produces a gap in the
    /// output (rather than resetting or interrupting the running total) - once data resumes, the
    /// total picks up from where it left off, same as the source series' own gaps do.
    /// </summary>
    public static IEnumerable<double?> CalculateCumulative(this IEnumerable<double?> values)
    {
        var result = new List<double?>();

        double runningTotal = 0;

        foreach (var value in values)
        {
            if (value == null)
            {
                result.Add(null);
                continue;
            }

            runningTotal += value.Value;

            result.Add(runningTotal);
        }

        return result;
    }
}
