namespace ClimateExplorer.CarbonCycle.Fuels;

using ClimateExplorer.CarbonCycle.Chemistry;

// One ideal fuel per representative molecule and one real fuel per row of the Tab 1 §5 fuel table (engine plan §5.3).
// Real-fuel values are provisional (Sources.ProvisionalFuel). Compositions are chosen so the derived carbon fraction is
// close to IPCC carbon content × NCV; Tab 1 step 2 sources them.
public static class DefaultFuels
{
    public static Fuel IdealCoal { get; } = Fuel.CreateIdeal("fuel.coal.ideal", "Coal as pure carbon", SpeciesCatalog.Carbon, null);

    public static Fuel IdealNaturalGas { get; } = Fuel.CreateIdeal("fuel.naturalGas.ideal", "Natural gas as methane", SpeciesCatalog.Methane, 0.717);

    public static Fuel IdealLpg { get; } = Fuel.CreateIdeal("fuel.lpg.ideal", "LPG as propane", SpeciesCatalog.Propane, 493);

    public static Fuel IdealPetrol { get; } = Fuel.CreateIdeal("fuel.petrol.ideal", "Petrol as octane", SpeciesCatalog.Octane, 703);

    public static Fuel IdealJetFuel { get; } = Fuel.CreateIdeal("fuel.jetFuel.ideal", "Jet fuel as dodecane", SpeciesCatalog.Dodecane, 750);

    public static Fuel IdealDiesel { get; } = Fuel.CreateIdeal("fuel.diesel.ideal", "Diesel as cetane", SpeciesCatalog.Cetane, 773);

    // Coals as received (ultimate analysis): carbon, hydrogen, oxygen, nitrogen, sulfur, moisture, ash
    public static Fuel Lignite { get; } = Fuel.CreateReal("fuel.lignite", "Lignite (brown coal)", Coal(0.328, 0.026, 0.105, 0.005, 0.008, 0.40, 0.128), 11.9e6, null, 0.003, IdealCoal);

    public static Fuel SubBituminousCoal { get; } = Fuel.CreateReal("fuel.subBituminousCoal", "Sub-bituminous coal", Coal(0.496, 0.036, 0.115, 0.007, 0.005, 0.26, 0.081), 18.9e6, null, 0.005, IdealCoal);

    public static Fuel BituminousCoal { get; } = Fuel.CreateReal("fuel.bituminousCoal", "Bituminous (black) coal", Coal(0.666, 0.042, 0.07, 0.014, 0.015, 0.08, 0.113), 25.8e6, null, 0.008, IdealCoal);

    public static Fuel Anthracite { get; } = Fuel.CreateReal("fuel.anthracite", "Anthracite (hard coal, almost pure carbon)", Coal(0.716, 0.02, 0.018, 0.008, 0.007, 0.08, 0.151), 26.7e6, null, 0.008, IdealCoal);

    public static Fuel NaturalGas { get; } = Fuel.CreateReal(
        "fuel.naturalGas",
        "Natural gas",
        [
            FuelComponent.Molecule("methane", "Methane", SpeciesCatalog.Methane, 0.90),
            FuelComponent.Molecule("ethane", "Ethane", SpeciesCatalog.Ethane, 0.075),
            FuelComponent.Molecule("carbonDioxide", "Carbon dioxide", SpeciesCatalog.CarbonDioxide, 0.01),
            FuelComponent.Molecule("nitrogen", "Nitrogen", SpeciesCatalog.Nitrogen, 0.015),
        ],
        48.0e6,
        0.75,
        0.003,
        IdealNaturalGas);

    public static Fuel Lpg { get; } = Fuel.CreateReal(
        "fuel.lpg",
        "LPG",
        [
            FuelComponent.Molecule("propane", "Propane", SpeciesCatalog.Propane, 0.6),
            FuelComponent.Molecule("butane", "Butane", SpeciesCatalog.Butane, 0.3999),
            FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, 0.0001),
        ],
        47.3e6,
        540,
        0.003,
        IdealLpg);

    public static Fuel Petrol { get; } = Fuel.CreateReal(
        "fuel.petrol",
        "Petrol",
        [
            FuelComponent.Molecule("alkanes", "Alkanes (as octane)", SpeciesCatalog.Octane, 0.74999),
            FuelComponent.Molecule("aromatics", "Aromatics (as toluene)", SpeciesCatalog.Toluene, 0.25),
            FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, 0.00001),
        ],
        44.3e6,
        740,
        0.008,
        IdealPetrol);

    public static Fuel JetFuel { get; } = Fuel.CreateReal(
        "fuel.jetFuel",
        "Jet fuel (Jet A-1 kerosene)",
        [
            FuelComponent.Molecule("alkanes", "Alkanes (as dodecane)", SpeciesCatalog.Dodecane, 0.7995),
            FuelComponent.Molecule("aromatics", "Aromatics (as toluene)", SpeciesCatalog.Toluene, 0.2),
            FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, 0.0005),
        ],
        44.1e6,
        800,
        0.014,
        IdealJetFuel);

    public static Fuel Diesel { get; } = Fuel.CreateReal(
        "fuel.diesel",
        "Diesel",
        [
            FuelComponent.Molecule("alkanes", "Alkanes (as cetane)", SpeciesCatalog.Cetane, 0.74999),
            FuelComponent.Molecule("aromatics", "Aromatics (as 1-methylnaphthalene)", SpeciesCatalog.Methylnaphthalene, 0.25),
            FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, 0.00001),
        ],
        43.0e6,
        835,
        0.015,
        IdealDiesel);

    // Heavy residue with no molecules to list; written as elements. No ideal counterpart.
    public static Fuel HeavyFuelOil { get; } = Fuel.CreateReal(
        "fuel.heavyFuelOil",
        "Bunker fuel (heavy fuel oil)",
        [
            FuelComponent.FromElement("carbon", "Carbon", Elements.C, 0.865),
            FuelComponent.FromElement("hydrogen", "Hydrogen", Elements.H, 0.114),
            FuelComponent.FromElement("oxygen", "Oxygen", Elements.O, 0.006),
            FuelComponent.FromElement("nitrogen", "Nitrogen", Elements.N, 0.004),
            FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, 0.005),
            FuelComponent.Moisture(0.005),
            FuelComponent.Ash(0.001),
        ],
        40.4e6,
        980,
        0.08);

    public static IReadOnlyList<Fuel> IdealFuels { get; } = [IdealCoal, IdealNaturalGas, IdealLpg, IdealPetrol, IdealJetFuel, IdealDiesel];

    public static IReadOnlyList<Fuel> RealFuels { get; } =
    [
        Lignite,
        SubBituminousCoal,
        BituminousCoal,
        NaturalGas,
        Lpg,
        Petrol,
        JetFuel,
        Diesel,
        HeavyFuelOil,
        Anthracite,
    ];

    public static IReadOnlyList<Fuel> All { get; } = [.. IdealFuels, .. RealFuels];

    private static List<FuelComponent> Coal(double carbon, double hydrogen, double oxygen, double nitrogen, double sulfur, double moisture, double ash) =>
    [
        FuelComponent.FromElement("carbon", "Carbon", Elements.C, carbon),
        FuelComponent.FromElement("hydrogen", "Hydrogen", Elements.H, hydrogen),
        FuelComponent.FromElement("oxygen", "Oxygen", Elements.O, oxygen),
        FuelComponent.FromElement("nitrogen", "Nitrogen", Elements.N, nitrogen),
        FuelComponent.FromElement("sulfur", "Sulfur", Elements.S, sulfur),
        FuelComponent.Moisture(moisture),
        FuelComponent.Ash(ash),
    ];
}
