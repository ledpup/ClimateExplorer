namespace ClimateExplorer.CarbonCycle.Chemistry;

// A reaction run for a known amount: the moles and mass of every participant, and the heat.
public sealed class ScaledReaction
{
    internal ScaledReaction(Reaction reaction, double extent)
    {
        Reaction = reaction;
        Extent = extent;
        Reactants = reaction.Reactants.Select(term => new ScaledTerm(term.Species, term.Coefficient * extent)).ToList();
        Products = reaction.Products.Select(term => new ScaledTerm(term.Species, term.Coefficient * extent)).ToList();
    }

    public Reaction Reaction { get; }

    // Moles of reaction as written
    public double Extent { get; }

    public IReadOnlyList<ScaledTerm> Reactants { get; }

    public IReadOnlyList<ScaledTerm> Products { get; }

    // J; negative when heat is released. Absent if the reaction has no enthalpy change.
    public double? EnthalpyChange => Reaction.EnthalpyChange * Extent * 1000;

    // The participant's amount, reactants first if it is on both sides.
    public ScaledTerm Of(Species species) =>
        Reactants.Concat(Products).FirstOrDefault(term => term.Species == species)
        ?? throw new ArgumentException($"{species.Id} is not in reaction {Reaction.Id}.", nameof(species));
}
