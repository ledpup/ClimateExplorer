namespace ClimateExplorer.CarbonCycle.Simulation;

// Effective radiative forcing, W/m², against a reference state (energy balance plan §3). Positive warms.
public static class RadiativeForcing
{
    // W/m² per doubling of CO₂ (Sources.CarbonDioxideForcing)
    public const double CarbonDioxidePerDoubling = 3.93;

    // Mole fraction. The simulator is about the warming people cause, so it doesn't go below preindustrial CO₂; lower
    // amounts are taken as this.
    public const double PreindustrialCarbonDioxide = 280e-6;

    // CO₂ at mole fraction c, against c₀. Logarithmic, so each doubling adds the same.
    public static double CarbonDioxide(double fraction, double referenceFraction) =>
        CarbonDioxidePerDoubling / Math.Log(2)
        * Math.Log(Math.Max(fraction, PreindustrialCarbonDioxide) / Math.Max(referenceFraction, PreindustrialCarbonDioxide));

    // The solar constant S against S₀, at albedo α: a quarter of the change, as the planet is a sphere, less what's reflected
    public static double Solar(double solarConstant, double referenceSolarConstant, double albedo) =>
        (solarConstant - referenceSolarConstant) / 4 * (1 - albedo);

    // Albedo α against α₀, under the reference sunlight. With the solar term, it sums to the change in absorbed sunlight.
    public static double Albedo(double referenceSolarConstant, double albedo, double referenceAlbedo) =>
        -referenceSolarConstant / 4 * (albedo - referenceAlbedo);
}
