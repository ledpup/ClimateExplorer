namespace ClimateExplorer.CarbonCycle.Fuels;

using ClimateExplorer.CarbonCycle.Chemistry;

public sealed record FuelComponent
{
    private FuelComponent(string id, string name, FuelComponentKind kind, Species? species, Element? element, CarbonOrigin origin, double massFraction)
    {
        Id = id;
        Name = name;
        Kind = kind;
        Species = species;
        Element = element;
        Origin = origin;
        MassFraction = massFraction;
    }

    // Unique within its fuel; the UI edits a component by this id, e.g. "aromatics", "sulfur", "moisture".
    public string Id { get; }

    public string Name { get; }

    public FuelComponentKind Kind { get; }

    // Molecule components only
    public Species? Species { get; }

    // Element components only
    public Element? Element { get; }

    public CarbonOrigin Origin { get; }

    // kg per kg of fuel, as received
    public double MassFraction { get; init; }

    public static FuelComponent Molecule(string id, string name, Species species, double massFraction, CarbonOrigin origin = CarbonOrigin.Fossil) =>
        new(id, name, FuelComponentKind.Molecule, species, null, origin, massFraction);

    public static FuelComponent FromElement(string id, string name, Element element, double massFraction) =>
        new(id, name, FuelComponentKind.Element, null, element, CarbonOrigin.Fossil, massFraction);

    public static FuelComponent Moisture(double massFraction) =>
        new("moisture", "Moisture", FuelComponentKind.Moisture, null, null, CarbonOrigin.Fossil, massFraction);

    public static FuelComponent Ash(double massFraction) =>
        new("ash", "Ash", FuelComponentKind.Ash, null, null, CarbonOrigin.Fossil, massFraction);

    // Moles of the element per kg of this component. Moisture and ash count as none: they don't burn.
    public double MolesPerKg(Element element) => Kind switch
    {
        FuelComponentKind.Molecule => (Species!.Formula.CountOf(element) * 1000) / Species.MolarMass,
        FuelComponentKind.Element => element == Element ? 1000 / element.StandardAtomicWeight : 0,
        _ => 0,
    };
}
