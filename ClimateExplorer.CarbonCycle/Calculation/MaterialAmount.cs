namespace ClimateExplorer.CarbonCycle.Calculation;

// Moles are absent for materials that are not one species: fuel, NMVOC and ash.
public sealed record MaterialAmount(Material Material, double MassKg, double? Moles);
