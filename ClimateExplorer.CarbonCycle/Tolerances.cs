namespace ClimateExplorer.CarbonCycle;

public static class Tolerances
{
    // Relative tolerance for atom balances and fraction sums (engine plan §7).
    public const double Relative = 1e-9;

    // Relative to the larger magnitude, with an absolute floor of Relative for values near zero.
    public static bool AreClose(double a, double b) =>
        Math.Abs(a - b) <= Relative * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
}
