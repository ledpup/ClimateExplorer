namespace ClimateExplorer.CarbonCycle.Simulation;

// Today's dry air by mole fraction (simulation plan §2.2). Argon makes up the rest. CO₂ and CH₄ are fixed for now; the
// plan takes them from ClimateExplorer's own records later.
public static class DryAir
{
    public const double Nitrogen = 0.78084;

    public const double Oxygen = 0.20946;

    public const double CarbonDioxide = 420e-6;

    public const double Methane = 1.9e-6;

    public const double Argon = 1 - Nitrogen - Oxygen - CarbonDioxide - Methane;
}
