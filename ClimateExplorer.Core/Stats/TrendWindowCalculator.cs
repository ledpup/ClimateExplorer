namespace ClimateExplorer.Core.Stats;

using ClimateExplorer.Core.Stats.Model;

public static class TrendWindowCalculator
{
    private const int MinimumMinimumCompletePoints = 6;

    public static TrendWindowSet? Calculate(
        IReadOnlyList<DataPoint> points,
        int minimumCompletePoints,
        int recentWindowSize,
        double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (minimumCompletePoints < MinimumMinimumCompletePoints)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCompletePoints), "Must be at least 6 so a first-half split still has 3 points.");
        }

        if (points.Count < minimumCompletePoints)
        {
            return null;
        }

        var ordered = points.OrderBy(p => p.X).ToList();
        var recentCount = Math.Min(recentWindowSize, ordered.Count);
        var firstHalfCount = ordered.Count / 2;

        return new TrendWindowSet(
            LinearRegressionCalculator.Calculate(ordered, alpha),
            LinearRegressionCalculator.Calculate(ordered.TakeLast(recentCount), alpha),
            LinearRegressionCalculator.Calculate(ordered.Take(firstHalfCount), alpha),
            ordered.Count);
    }
}
