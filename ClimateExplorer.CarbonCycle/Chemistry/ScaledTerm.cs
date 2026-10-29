namespace ClimateExplorer.CarbonCycle.Chemistry;

public sealed record ScaledTerm(Species Species, double Moles)
{
    public double MassKg => (Moles * Species.MolarMass) / 1000;
}
