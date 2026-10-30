namespace ClimateExplorer.CarbonCycle.Calculation;

using ClimateExplorer.CarbonCycle.Chemistry;

// EnthalpyChange is in J, negative when heat is released.
// Reactions are for display: see CombustionReactions.ForFuel for their order.
public sealed record Tab1Stream(
    Tab1StreamKind Kind,
    IReadOnlyList<MaterialAmount> Reactants,
    IReadOnlyList<MaterialAmount> Products,
    double EnthalpyChange,
    CarbonByFate CarbonByFate,
    IReadOnlyList<Reaction> Reactions)
{
    // kg; zero if the material is not a reactant
    public double ReactantMass(Material material) => Reactants.Where(amount => amount.Material == material).Sum(amount => amount.MassKg);

    public double ReactantMoles(Material material) => Reactants.Where(amount => amount.Material == material).Sum(amount => amount.Moles ?? 0);

    // kg; zero if the material is not a product
    public double ProductMass(Material material) => Products.Where(amount => amount.Material == material).Sum(amount => amount.MassKg);

    public double ProductMoles(Material material) => Products.Where(amount => amount.Material == material).Sum(amount => amount.Moles ?? 0);
}
