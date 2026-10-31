namespace ClimateExplorer.CarbonCycle.Fuels;

public enum FuelKind
{
    // One pure molecule. Everything comes from the catalog; fixed, not editable.
    Ideal,

    // The mixture actually sold and burned. Composition and heating value are editable assumptions.
    Real,
}
