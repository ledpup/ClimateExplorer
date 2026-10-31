namespace ClimateExplorer.CarbonCycle.Fuels;

using ClimateExplorer.CarbonCycle.Chemistry;

// A fuel is a mixture of components, each with a mass fraction (engine plan §5). Instances are immutable and valid:
// the factories and the With methods reject bad input with an InputValidationException.
public sealed class Fuel
{
    private static readonly Element[] CombustibleElements = [Elements.C, Elements.H, Elements.O, Elements.N, Elements.S];

    private readonly Dictionary<Element, double> molesPerKg;

    private Fuel(
        string id,
        string name,
        FuelKind kind,
        Fuel? idealCounterpart,
        IReadOnlyList<FuelComponent> components,
        double? netCalorificValue,
        double? density,
        double noxFactor)
    {
        Id = id;
        Name = name;
        Kind = kind;
        IdealCounterpart = idealCounterpart;
        Components = components;
        Density = density;
        NoxFactor = noxFactor;

        molesPerKg = CombustibleElements.ToDictionary(element => element, element => components.Sum(component => component.MassFraction * component.MolesPerKg(element)));

        var carbonMolesPerKg = molesPerKg[Elements.C];
        var fastCycleCarbonMolesPerKg = components
            .Where(component => component.Origin == CarbonOrigin.FastCycle)
            .Sum(component => component.MassFraction * component.MolesPerKg(Elements.C));

        CarbonMassFraction = (carbonMolesPerKg * Elements.C.StandardAtomicWeight) / 1000;
        SulfurMassFraction = (molesPerKg[Elements.S] * Elements.S.StandardAtomicWeight) / 1000;
        MoistureMassFraction = components.Where(component => component.Kind == FuelComponentKind.Moisture).Sum(component => component.MassFraction);
        AshMassFraction = components.Where(component => component.Kind == FuelComponentKind.Ash).Sum(component => component.MassFraction);
        HydrogenToCarbonRatio = molesPerKg[Elements.H] / carbonMolesPerKg;
        FastCycleCarbonShare = fastCycleCarbonMolesPerKg / carbonMolesPerKg;
        PerCarbonFormula = ChemicalFormula.FromAtoms(CombustibleElements.Select(element => new FormulaAtom(element, molesPerKg[element] / carbonMolesPerKg)));
        DerivedNetCalorificValue = DeriveNetCalorificValue(components);
        NetCalorificValue = netCalorificValue ?? DerivedNetCalorificValue!.Value;
    }

    // e.g. "fuel.petrol.ideal", "fuel.petrol"
    public string Id { get; }

    public string Name { get; }

    public FuelKind Kind { get; }

    // Real fuels only: the ideal fuel shown beside it (petrol → octane, coal → carbon)
    public Fuel? IdealCounterpart { get; }

    public IReadOnlyList<FuelComponent> Components { get; }

    // J/kg, water as vapour. Ideal fuel: derived from the catalog (a Constant). Real fuel: an Assumption.
    public double NetCalorificValue { get; }

    // kg/m³ (a liquid's kg/L × 1000; a gas at 0 °C and 1 atm). Absent for solids, which are entered by mass.
    public double? Density { get; }

    // kg NOx (as NO₂) per kg of fuel. It comes mostly from the air's nitrogen, so it's a factor, not a product of the
    // fuel's composition (engine plan §2 item 10).
    public double NoxFactor { get; }

    public double CarbonMassFraction { get; }

    public double SulfurMassFraction { get; }

    public double MoistureMassFraction { get; }

    public double AshMassFraction { get; }

    // Atomic H:C of the fuel matter, not counting the hydrogen in moisture
    public double HydrogenToCarbonRatio { get; }

    // Share of the fuel's carbon that is fast-cycle
    public double FastCycleCarbonShare { get; }

    // CH_y O_z N_n S_w, per atom of carbon; for display (engine plan §5.2)
    public ChemicalFormula PerCarbonFormula { get; }

    // J/kg, added up from the components' enthalpies of formation. Absent where a component has no molecule or no
    // enthalpy (coal, or diesel's methylnaphthalene). For a real fuel it is a check shown at Level 2, never the result.
    public double? DerivedNetCalorificValue { get; }

    // Ideal fuels only
    public Species? Molecule => Kind == FuelKind.Ideal ? Components[0].Species : null;

    public static Fuel CreateIdeal(string id, string name, Species molecule, double? density, double noxFactor = 0)
    {
        if (molecule.EnthalpyOfFormation is null)
        {
            throw new ArgumentException($"An ideal fuel's molecule needs an enthalpy of formation; {molecule.Id} has none.", nameof(molecule));
        }

        if (CombustionReactions.Complete(molecule)?.EnthalpyChange is not < 0)
        {
            throw new ArgumentException($"{molecule.Id} does not release heat when it burns.", nameof(molecule));
        }

        List<FuelComponent> components = [FuelComponent.Molecule(molecule.Id, molecule.Name, molecule, 1)];

        ThrowIfInvalid(components, null, density, noxFactor);

        return new Fuel(id, name, FuelKind.Ideal, null, components, null, density, noxFactor);
    }

    public static Fuel CreateReal(
        string id,
        string name,
        IReadOnlyList<FuelComponent> components,
        double netCalorificValue,
        double? density,
        double noxFactor,
        Fuel? idealCounterpart = null)
    {
        ThrowIfInvalid(components, netCalorificValue, density, noxFactor);

        return new Fuel(id, name, FuelKind.Real, idealCounterpart, components, netCalorificValue, density, noxFactor);
    }

    // Moles of the element per kg of fuel as received, not counting moisture
    public double MolesPerKg(Element element) => molesPerKg.GetValueOrDefault(element);

    // Sets one component's fraction. The largest other component takes up the difference so the fractions still sum
    // to 1; if it can't, the others are rescaled in proportion.
    public Fuel WithComponentFraction(string componentId, double massFraction)
    {
        if (Kind == FuelKind.Ideal)
        {
            throw new InvalidOperationException("An ideal fuel is one pure molecule; its composition is fixed.");
        }

        var index = Components.ToList().FindIndex(component => component.Id == componentId);

        if (index < 0)
        {
            throw new ArgumentException($"Fuel {Id} has no component '{componentId}'.", nameof(componentId));
        }

        var errors = new List<ValidationError>();
        InputValidation.Fraction(errors, $"fuel.components.{componentId}", massFraction);
        InputValidationException.ThrowIfAny(errors);

        var fractions = Components.Select(component => component.MassFraction).ToArray();
        fractions[index] = massFraction;

        var others = Enumerable.Range(0, fractions.Length).Where(i => i != index).ToList();
        var othersTarget = 1 - massFraction;

        if (others.Count == 0)
        {
            if (!Tolerances.AreClose(massFraction, 1))
            {
                throw new InputValidationException($"fuel.components.{componentId}", "is the only component, so it must be 1.");
            }
        }
        else
        {
            var othersSum = others.Sum(i => fractions[i]);
            var largest = others.MaxBy(i => fractions[i]);
            var difference = othersTarget - othersSum;

            if (fractions[largest] + difference >= 0)
            {
                fractions[largest] += difference;
            }
            else
            {
                foreach (var i in others)
                {
                    fractions[i] *= othersTarget / othersSum;
                }
            }
        }

        var components = Components.Select((component, i) => component with { MassFraction = fractions[i] }).ToList();

        return CreateReal(Id, Name, components, NetCalorificValue, Density, NoxFactor, IdealCounterpart);
    }

    public Fuel WithNetCalorificValue(double netCalorificValue)
    {
        if (Kind == FuelKind.Ideal)
        {
            throw new InvalidOperationException("An ideal fuel's heating value is derived from the chemistry; it is fixed.");
        }

        return CreateReal(Id, Name, Components, netCalorificValue, Density, NoxFactor, IdealCounterpart);
    }

    public Fuel WithDensity(double? density) =>
        Kind == FuelKind.Ideal
            ? CreateIdeal(Id, Name, Molecule!, density, NoxFactor)
            : CreateReal(Id, Name, Components, NetCalorificValue, density, NoxFactor, IdealCounterpart);

    public Fuel WithNoxFactor(double noxFactor) =>
        Kind == FuelKind.Ideal
            ? CreateIdeal(Id, Name, Molecule!, Density, noxFactor)
            : CreateReal(Id, Name, Components, NetCalorificValue, Density, noxFactor, IdealCounterpart);

    public override string ToString() => Id;

    private static void ThrowIfInvalid(IReadOnlyList<FuelComponent> components, double? netCalorificValue, double? density, double noxFactor)
    {
        var errors = new List<ValidationError>();

        if (components.Count == 0)
        {
            errors.Add(new ValidationError("fuel.components", "a fuel needs at least one component."));
        }

        if (components.Select(component => component.Id).Distinct().Count() != components.Count)
        {
            errors.Add(new ValidationError("fuel.components", "component ids must be unique."));
        }

        foreach (var component in components)
        {
            InputValidation.Fraction(errors, $"fuel.components.{component.Id}", component.MassFraction);

            if (component.Element is Element element && !CombustibleElements.Contains(element))
            {
                errors.Add(new ValidationError($"fuel.components.{component.Id}", $"{element.Symbol} can't be a fuel element; use C, H, O, N or S."));
            }
        }

        InputValidation.SumsToOne(errors, "fuel.components", components.Select(component => component.MassFraction));

        if (components.Sum(component => component.MassFraction * component.MolesPerKg(Elements.C)) <= 0)
        {
            errors.Add(new ValidationError("fuel.components", "a fuel must contain carbon."));
        }

        if (netCalorificValue is double value)
        {
            InputValidation.Positive(errors, "fuel.netCalorificValue", value);
        }

        if (density is double densityValue)
        {
            InputValidation.Positive(errors, "fuel.density", densityValue);
        }

        InputValidation.NonNegative(errors, "fuel.noxFactor", noxFactor);

        InputValidationException.ThrowIfAny(errors);
    }

    // J/kg. Molecules burn by their generated complete-combustion reaction (one with nothing to burn, such as CO₂ or N₂,
    // adds nothing); element sulfur by S + O₂ → SO₂; moisture costs its latent heat; ash adds nothing.
    private static double? DeriveNetCalorificValue(IReadOnlyList<FuelComponent> components)
    {
        double total = 0;

        foreach (var component in components)
        {
            switch (component.Kind)
            {
                case FuelComponentKind.Molecule:
                    var reaction = CombustionReactions.Complete(component.Species!);

                    if (reaction is null)
                    {
                        break;
                    }

                    if (reaction.EnthalpyChange is not double enthalpyChange)
                    {
                        return null;
                    }

                    total += component.MassFraction * HeatReleasedPerKg(enthalpyChange, component.Species!);
                    break;

                case FuelComponentKind.Element:
                    if (component.Element != Elements.S)
                    {
                        return null;
                    }

                    total += component.MassFraction * HeatReleasedPerKg(ReactionCatalog.SulfurCombustion.EnthalpyChange!.Value, SpeciesCatalog.Sulfur);
                    break;

                case FuelComponentKind.Moisture:
                    var evaporation = SpeciesCatalog.WaterVapour.EnthalpyOfFormation!.Value - SpeciesCatalog.LiquidWater.EnthalpyOfFormation!.Value;
                    total += component.MassFraction * HeatReleasedPerKg(evaporation, SpeciesCatalog.LiquidWater);
                    break;
            }
        }

        return total;
    }

    // kJ/mol ÷ g/mol = MJ/kg
    private static double HeatReleasedPerKg(double enthalpyChangePerMole, Species species) => (-enthalpyChangePerMole / species.MolarMass) * 1e6;
}
