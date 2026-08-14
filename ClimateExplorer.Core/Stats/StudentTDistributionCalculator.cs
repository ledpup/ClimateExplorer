namespace ClimateExplorer.Core.Stats;

internal static class StudentTDistributionCalculator
{
    public static double TwoTailedPValue(double tStatistic, int degreesOfFreedom)
    {
        if (degreesOfFreedom <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degreesOfFreedom),
                "Degrees of freedom must be positive.");
        }

        var t = Math.Abs(tStatistic);
        var x = degreesOfFreedom / (degreesOfFreedom + (t * t));

        return RegularizedIncompleteBetaFunction.Calculate(x, degreesOfFreedom / 2.0, 0.5);
    }

    public static double TwoTailedCriticalValue(double alpha, int degreesOfFreedom)
    {
        if (degreesOfFreedom <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degreesOfFreedom),
                "Degrees of freedom must be positive.");
        }

        var lower = 0.0;
        var upper = 1.0;

        while (TwoTailedPValue(upper, degreesOfFreedom) > alpha)
        {
            upper *= 2;
        }

        for (var i = 0; i < 100; i++)
        {
            var middle = (lower + upper) / 2;

            if (TwoTailedPValue(middle, degreesOfFreedom) > alpha)
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }
        }

        return (lower + upper) / 2;
    }
}
