namespace ClimateExplorer.CarbonCycle.Chemistry;

using static ClimateExplorer.CarbonCycle.Chemistry.ReactionCatalog;

public static class MechanismCatalog
{
    // Tab 1 plan §3
    public static Mechanism ZeldovichNitricOxide { get; } = new("mechanism.zeldovich", "Zeldovich NO formation", [new(ZeldovichOxygenAtom, 1), new(ZeldovichNitrogenAtom, 1)], ThermalNitricOxide);

    // Tab 1 plan §3; NOx and OH are regenerated
    public static Mechanism CarbonMonoxideOzone { get; } = new(
        "mechanism.carbonMonoxideOzone",
        "Ozone formation from CO",
        [
            new(CarbonMonoxideHydroxyl, 1),
            new(HydroperoxylFormation, 1),
            new(HydroperoxylNitricOxide, 1),
            new(NitrogenDioxidePhotolysis, 1),
            new(OzoneFormation, 1),
        ],
        CarbonMonoxideToOzone);

    // Tab 1 plan §4
    public static Mechanism SulfateGasPhaseRoute { get; } = new(
        "mechanism.sulfateGasPhase",
        "SO₂ to sulfate, gas phase",
        [
            new(SulfurDioxideHydroxyl, 1),
            new(HydroxysulfonylOxygen, 1),
            new(SulfurTrioxideHydration, 1),
        ],
        SulfateGasPhase);

    public static IReadOnlyList<Mechanism> All { get; } = [ZeldovichNitricOxide, CarbonMonoxideOzone, SulfateGasPhaseRoute];
}
