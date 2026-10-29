namespace ClimateExplorer.CarbonCycle.Fuels;

public enum FuelComponentKind
{
    // A catalog species standing for a class, e.g. octane for petrol's alkanes
    Molecule,

    // One of C, H, O, N, S, for fuels with no molecules to list (coal's ultimate analysis)
    Element,

    // Leaves as water vapour
    Moisture,

    // Passes through unchanged
    Ash,
}
