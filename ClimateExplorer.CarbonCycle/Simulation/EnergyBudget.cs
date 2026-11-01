namespace ClimateExplorer.CarbonCycle.Simulation;

// A planet's energy budget at one moment, W/m² of its surface (energy balance plan §2)
public sealed record EnergyBudget(
    double IncomingSunlight,
    double Reflected,
    double Absorbed,
    double HeatToSpace,
    double NonRadiativeHeat)
{
    // N: positive while the planet takes in more than it sends to space. The non-radiative heat isn't in it.
    public double Imbalance => Absorbed - HeatToSpace;
}
