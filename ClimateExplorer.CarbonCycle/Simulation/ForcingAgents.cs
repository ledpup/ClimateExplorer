namespace ClimateExplorer.CarbonCycle.Simulation;

// The forcing agents built so far. CH₄, N₂O, halogenated gases, aerosols and land use come later (energy balance plan §3).
public static class ForcingAgents
{
    public static ForcingAgent CarbonDioxide { get; } = new("co2", "Carbon dioxide", ForcingKind.GreenhouseGas, Sources.CarbonDioxideForcing);

    public static ForcingAgent Solar { get; } = new("solar", "Sunlight", ForcingKind.Solar, Sources.SolarForcing);

    public static ForcingAgent Albedo { get; } = new("albedo", "Albedo", ForcingKind.Albedo, Sources.AlbedoForcing);

    public static IReadOnlyList<ForcingAgent> All { get; } = [CarbonDioxide, Solar, Albedo];
}
