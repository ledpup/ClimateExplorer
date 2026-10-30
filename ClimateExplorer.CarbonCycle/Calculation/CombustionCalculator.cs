namespace ClimateExplorer.CarbonCycle.Calculation;

using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;

// Fuel combustion with a carbon split (engine plan §6.1, §6.2).
public static class CombustionCalculator
{
    public static Tab1Stream Burn(Fuel fuel, double fuelMassKg, CarbonSplit split)
    {
        var errors = new List<ValidationError>();
        InputValidation.NonNegative(errors, "fuelMass", fuelMassKg);
        split.Validate(errors);
        InputValidationException.ThrowIfAny(errors);

        // Unburnt fuel is a slice of the whole fuel, so its hydrogen and sulfur stay unburnt too.
        var burntMass = fuelMassKg * split.Burnt;
        var unburntMass = fuelMassKg * split.Unburnt;

        var carbon = fuel.MolesPerKg(Elements.C) * fuelMassKg;
        var carbonToCarbonDioxide = carbon * split.CarbonDioxide;
        var carbonToCarbonMonoxide = carbon * split.CarbonMonoxide;
        var carbonToSoot = carbon * split.Soot;
        var carbonUnburnt = carbon * split.Unburnt;

        // The burnt remainder as element totals (mol)
        var hydrogen = fuel.MolesPerKg(Elements.H) * burntMass;
        var fuelOxygen = fuel.MolesPerKg(Elements.O) * burntMass;
        var nitrogen = fuel.MolesPerKg(Elements.N) * burntMass;
        var sulfur = fuel.MolesPerKg(Elements.S) * burntMass;

        // O₂ from the air; oxygen already in the fuel lowers the demand.
        var oxygen = carbonToCarbonDioxide + (0.5 * carbonToCarbonMonoxide) + (0.25 * hydrogen) + sulfur - (0.5 * fuelOxygen);

        if (oxygen < -Tolerances.Relative * Math.Max(1, carbon))
        {
            throw new InputValidationException("carbonSplit", "the fuel holds more oxygen than this carbon split can use.");
        }

        oxygen = Math.Max(oxygen, 0);

        var moistureMass = fuel.MoistureMassFraction * fuelMassKg;
        var water = (0.5 * hydrogen) + Moles(moistureMass, SpeciesCatalog.WaterVapour);
        var (unburntMethane, nmvoc) = SplitUnburnt(fuel, unburntMass);

        List<MaterialAmount> reactants = [new(Material.Fuel, fuelMassKg, null)];
        Add(reactants, Material.Oxygen, oxygen, SpeciesCatalog.Oxygen);

        var products = new List<MaterialAmount>();
        Add(products, Material.CarbonDioxide, carbonToCarbonDioxide, SpeciesCatalog.CarbonDioxide);
        Add(products, Material.Water, water, SpeciesCatalog.WaterVapour);
        Add(products, Material.CarbonMonoxide, carbonToCarbonMonoxide, SpeciesCatalog.CarbonMonoxide);
        Add(products, Material.Soot, carbonToSoot, SpeciesCatalog.Carbon);
        Add(products, Material.UnburntMethane, Moles(unburntMethane, SpeciesCatalog.Methane), SpeciesCatalog.Methane);
        AddMass(products, Material.Nmvoc, nmvoc);
        Add(products, Material.SulfurDioxide, sulfur, SpeciesCatalog.SulfurDioxide);

        // NOx is a factor on the whole fuel, independent of the split, and takes no oxygen from the balance.
        Add(products, Material.Nox, Moles(fuel.NoxFactor * fuelMassKg, SpeciesCatalog.NitrogenDioxide), SpeciesCatalog.NitrogenDioxide);
        Add(products, Material.Nitrogen, 0.5 * nitrogen, SpeciesCatalog.Nitrogen);
        AddMass(products, Material.Ash, fuel.AshMassFraction * fuelMassKg);

        // Heat: the burnt fuel at its heating value, less the heat not released by carbon that stopped at CO or soot.
        var heatNotReleasedPerMoleCarbonMonoxide = -1000 * ReactionCatalog.CarbonMonoxideOxidation.EnthalpyChange!.Value;
        var heatNotReleasedPerMoleSoot = -1000 * ReactionCatalog.CarbonCombustion.EnthalpyChange!.Value;
        var enthalpyChange = -(burntMass * fuel.NetCalorificValue)
            + (carbonToCarbonMonoxide * heatNotReleasedPerMoleCarbonMonoxide)
            + (carbonToSoot * heatNotReleasedPerMoleSoot);

        var fastCycleShare = fuel.FastCycleCarbonShare;
        var carbonByFate = new CarbonByFate(
            CarbonAmount.Split(CarbonMass(carbonToCarbonDioxide), fastCycleShare),
            CarbonAmount.Split(CarbonMass(carbonToCarbonMonoxide), fastCycleShare),
            CarbonAmount.Split(CarbonMass(carbonToSoot), fastCycleShare),
            CarbonAmount.Split(CarbonMass(carbonUnburnt), fastCycleShare));

        return new Tab1Stream(Tab1StreamKind.Combustion, reactants, products, enthalpyChange, carbonByFate, CombustionReactions.ForFuel(fuel, split));
    }

    // Unburnt fuel by component: methane as CH₄; other molecules and element-written fuels as NMVOC. The slice's
    // moisture and ash are not fuel: they leave as water and ash with the rest.
    private static (double MethaneKg, double NmvocKg) SplitUnburnt(Fuel fuel, double unburntMass)
    {
        double methane = 0;
        double nmvoc = 0;

        foreach (var component in fuel.Components)
        {
            var mass = component.MassFraction * unburntMass;

            if (component.Kind == FuelComponentKind.Molecule && component.Species == SpeciesCatalog.Methane)
            {
                methane += mass;
            }
            else if (component.Kind is FuelComponentKind.Molecule or FuelComponentKind.Element)
            {
                nmvoc += mass;
            }
        }

        return (methane, nmvoc);
    }

    private static double Moles(double massKg, Species species) => (massKg * 1000) / species.MolarMass;

    private static double CarbonMass(double moles) => (moles * Elements.C.StandardAtomicWeight) / 1000;

    private static void Add(List<MaterialAmount> amounts, Material material, double moles, Species species)
    {
        if (moles > 0)
        {
            amounts.Add(new MaterialAmount(material, (moles * species.MolarMass) / 1000, moles));
        }
    }

    private static void AddMass(List<MaterialAmount> amounts, Material material, double massKg)
    {
        if (massKg > 0)
        {
            amounts.Add(new MaterialAmount(material, massKg, null));
        }
    }
}
