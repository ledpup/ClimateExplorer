namespace ClimateExplorer.CarbonCycle.Chemistry;

// Multiple is how many times the step runs per mole of the mechanism's net reaction.
public sealed record MechanismStep(Reaction Reaction, double Multiple);
