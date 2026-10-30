namespace ClimateExplorer.CarbonCycle.Chemistry;

// A chemical species is identified by formula and phase: H₂O(g) and H₂O(l) are two species.
// Catalog species are singletons, so equality is by reference.
public sealed class Species
{
    public Species(string id, string name, string formula, Phase phase, double? enthalpyOfFormation, string source)
        : this(id, name, ChemicalFormula.Parse(formula), phase, enthalpyOfFormation, source)
    {
    }

    public Species(string id, string name, ChemicalFormula formula, Phase phase, double? enthalpyOfFormation, string source)
    {
        Id = id;
        Name = name;
        Formula = formula;
        Phase = phase;
        EnthalpyOfFormation = enthalpyOfFormation;
        Source = source;
    }

    // Stable key, e.g. "CO2(g)", "H2O(l)", "C(s,graphite)"
    public string Id { get; }

    public string Name { get; }

    public ChemicalFormula Formula { get; }

    public Phase Phase { get; }

    // kJ/mol at 25 °C, standard state. Absent for species that are only shown, such as radicals.
    public double? EnthalpyOfFormation { get; }

    public string Source { get; }

    // g/mol
    public double MolarMass => Formula.MolarMass;

    public string PhaseLabel => Phase switch
    {
        Phase.Gas => "g",
        Phase.Liquid => "l",
        Phase.Solid => "s",
        Phase.Aqueous => "aq",
        _ => throw new InvalidOperationException($"Unknown phase {Phase}."),
    };

    public string ToDisplayString(bool includePhase = false) =>
        includePhase ? $"{Formula.ToDisplayString()}({PhaseLabel})" : Formula.ToDisplayString();

    public override string ToString() => Id;
}
