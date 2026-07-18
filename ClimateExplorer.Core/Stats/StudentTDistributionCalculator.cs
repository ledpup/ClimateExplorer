namespace ClimateExplorer.Core.Stats;

internal static class StudentTDistributionCalculator
{
    private static readonly double[] LanczosCoefficients =
    [
        676.5203681218851,
        -1259.1392167224028,
        771.32342877765313,
        -176.61502916214059,
        12.507343278686905,
        -0.13857109526572012,
        9.9843695780195716e-6,
        1.5056327351493116e-7,
    ];

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

        return RegularizedIncompleteBeta(x, degreesOfFreedom / 2.0, 0.5);
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

    private static double RegularizedIncompleteBeta(double x, double a, double b)
    {
        if (x is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "x must be between 0 and 1.");
        }

        if (x == 0 || x == 1)
        {
            return x;
        }

        var logBetaTerm =
            LogGamma(a + b)
            - LogGamma(a)
            - LogGamma(b)
            + (a * Math.Log(x))
            + (b * Math.Log(1 - x));
        var betaTerm = Math.Exp(logBetaTerm);

        if (x < (a + 1) / (a + b + 2))
        {
            return betaTerm * BetaContinuedFraction(a, b, x) / a;
        }

        return 1 - (betaTerm * BetaContinuedFraction(b, a, 1 - x) / b);
    }

    private static double BetaContinuedFraction(double a, double b, double x)
    {
        const int maximumIterations = 200;
        const double epsilon = 3e-14;
        const double floor = double.Epsilon / epsilon;

        var qab = a + b;
        var qap = a + 1;
        var qam = a - 1;
        var c = 1.0;
        var d = 1 - (qab * x / qap);

        if (Math.Abs(d) < floor)
        {
            d = floor;
        }

        d = 1 / d;
        var h = d;

        for (var m = 1; m <= maximumIterations; m++)
        {
            var m2 = 2 * m;
            var aa = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = 1 + (aa * d);

            if (Math.Abs(d) < floor)
            {
                d = floor;
            }

            c = 1 + (aa / c);

            if (Math.Abs(c) < floor)
            {
                c = floor;
            }

            d = 1 / d;
            h *= d * c;
            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = 1 + (aa * d);

            if (Math.Abs(d) < floor)
            {
                d = floor;
            }

            c = 1 + (aa / c);

            if (Math.Abs(c) < floor)
            {
                c = floor;
            }

            d = 1 / d;
            var delta = d * c;
            h *= delta;

            if (Math.Abs(delta - 1) < epsilon)
            {
                break;
            }
        }

        return h;
    }

    private static double LogGamma(double z)
    {
        if (z < 0.5)
        {
            return Math.Log(Math.PI) - Math.Log(Math.Sin(Math.PI * z)) - LogGamma(1 - z);
        }

        z -= 1;
        var x = 0.99999999999980993;

        for (var i = 0; i < LanczosCoefficients.Length; i++)
        {
            x += LanczosCoefficients[i] / (z + i + 1);
        }

        var t = z + LanczosCoefficients.Length - 0.5;

        return (0.5 * Math.Log(2 * Math.PI)) + ((z + 0.5) * Math.Log(t)) - t + Math.Log(x);
    }
}
