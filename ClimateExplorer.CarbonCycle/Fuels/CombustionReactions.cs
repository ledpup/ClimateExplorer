namespace ClimateExplorer.CarbonCycle.Fuels;

using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;

// Reactions that are not in any table, built from a composition and a carbon split (engine plan §2 item 9, §5.2).
public static class CombustionReactions
{
    // Complete combustion of one mole of a CₓHᵧO_zN_nS_w molecule:
    // CₓHᵧO_zN_nS_w + (x + y/4 + w − z/2) O₂ → x CO₂ + (y/2) H₂O + (n/2) N₂ + w SO₂.
    // Absent if there is nothing to burn (N₂) or it is already fully oxidised (CO₂).
    public static Reaction? Complete(Species molecule) =>
        Generate(molecule, CarbonSplit.Complete, $"combustion.{molecule.Id}.complete", $"{molecule.Name} burns completely");

    // One mole of the species (an ideal fuel's molecule, or a real fuel's per-carbon formula) burned at the split.
    // Unburnt fuel appears as a product, so the equation balances with the split applied.
    public static Reaction? WithCarbonSplit(Species fuel, CarbonSplit split) =>
        Generate(fuel, split, $"combustion.{fuel.Id}.split", $"{fuel.Name} burns");

    // The fuel's reactions for display: first its overall reaction at the split (per mole of molecule for an ideal fuel,
    // per mole of carbon for a real one), then, for a real fuel, each molecular component's complete combustion.
    public static IReadOnlyList<Reaction> ForFuel(Fuel fuel, CarbonSplit split)
    {
        if (fuel.Kind == FuelKind.Ideal)
        {
            return [WithCarbonSplit(fuel.Molecule!, split)!];
        }

        List<Reaction> reactions = [WithCarbonSplit(PerCarbonSpecies(fuel), split)!];

        reactions.AddRange(fuel.Components
            .Where(component => component.Kind == FuelComponentKind.Molecule)
            .Select(component => Complete(component.Species!))
            .OfType<Reaction>());

        return reactions;
    }

    // The real fuel's per-carbon formula as a generated species. It is not in the catalog and has no enthalpy.
    public static Species PerCarbonSpecies(Fuel fuel)
    {
        var phase = fuel.Components
            .Where(component => component.Kind == FuelComponentKind.Molecule)
            .MaxBy(component => component.MassFraction)?.Species!.Phase
            ?? Phase.Solid;

        return new Species($"{fuel.Id}.perCarbon", $"{fuel.Name} (per carbon)", fuel.PerCarbonFormula, phase, null, Sources.Generated);
    }

    private static Reaction? Generate(Species fuel, CarbonSplit split, string id, string name)
    {
        var formula = fuel.Formula;
        var unsupported = formula.Atoms.Select(atom => atom.Element).Except([Elements.C, Elements.H, Elements.O, Elements.N, Elements.S]).ToList();

        if (unsupported.Count > 0)
        {
            throw new ArgumentException($"{fuel.Id} contains {string.Join(", ", unsupported)}; a fuel is made of C, H, O, N and S.", nameof(fuel));
        }

        var carbon = formula.CountOf(Elements.C);
        var hydrogen = formula.CountOf(Elements.H);
        var oxygen = formula.CountOf(Elements.O);
        var nitrogen = formula.CountOf(Elements.N);
        var sulfur = formula.CountOf(Elements.S);

        if (carbon == 0 && hydrogen == 0 && sulfur == 0)
        {
            return null;
        }

        var burnt = split.Burnt;
        var oxygenDemand = (carbon * split.CarbonDioxide) + (0.5 * carbon * split.CarbonMonoxide) + (burnt * ((hydrogen / 4) + sulfur - (oxygen / 2)));

        if (split == CarbonSplit.Complete && oxygenDemand <= Tolerances.Relative)
        {
            return null;
        }

        var reactants = new List<ReactionTerm>();
        var products = new List<ReactionTerm>();

        AddTerm(reactants, 1, fuel);
        AddTerm(reactants, oxygenDemand, SpeciesCatalog.Oxygen);
        AddTerm(products, carbon * split.CarbonDioxide, SpeciesCatalog.CarbonDioxide);
        AddTerm(products, carbon * split.CarbonMonoxide, SpeciesCatalog.CarbonMonoxide);
        AddTerm(products, carbon * split.Soot, SpeciesCatalog.Carbon);
        AddTerm(products, (burnt * hydrogen) / 2, SpeciesCatalog.WaterVapour);
        AddTerm(products, (burnt * nitrogen) / 2, SpeciesCatalog.Nitrogen);
        AddTerm(products, burnt * sulfur, SpeciesCatalog.SulfurDioxide);
        AddTerm(products, split.Unburnt, fuel);

        // A fuel richer in oxygen than the split can use gives some back as O₂.
        AddTerm(products, -oxygenDemand, SpeciesCatalog.Oxygen);

        return new Reaction(id, name, reactants, products);
    }

    // Skips amounts that are zero or negative, and merges a species already on that side.
    private static void AddTerm(List<ReactionTerm> terms, double coefficient, Species species)
    {
        if (coefficient <= Tolerances.Relative)
        {
            return;
        }

        var index = terms.FindIndex(term => term.Species == species);

        if (index < 0)
        {
            terms.Add(new ReactionTerm(coefficient, species));
        }
        else
        {
            terms[index] = terms[index] with { Coefficient = terms[index].Coefficient + coefficient };
        }
    }
}
