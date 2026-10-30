namespace ClimateExplorer.CarbonCycle.Chemistry;

// Count is whole for catalog species; a real fuel's generated per-carbon formula has fractional counts.
public sealed record FormulaAtom(Element Element, double Count);
