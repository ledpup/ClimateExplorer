namespace ClimateExplorer.CarbonCycle.Chemistry;

// StandardAtomicWeight is in g/mol.
public sealed record Element(string Symbol, string Name, int AtomicNumber, double StandardAtomicWeight, string Source)
{
    public override string ToString() => Symbol;
}
