namespace ClimateExplorer.CarbonCycle.Chemistry;

using static ClimateExplorer.CarbonCycle.Chemistry.SpeciesCatalog;

// Reaction energies are derived from the species' enthalpies of formation, never stored (engine plan §2 item 5).
public static class ReactionCatalog
{
    public static Reaction CarbonCombustion { get; } = Create("combustion.carbon.complete", "Carbon burns to CO₂", [(1, Carbon), (1, Oxygen)], [(1, CarbonDioxide)]);

    public static Reaction CarbonToCarbonMonoxide { get; } = Create("combustion.carbon.carbonMonoxide", "Carbon burns to CO", [(1, Carbon), (0.5, Oxygen)], [(1, CarbonMonoxide)]);

    public static Reaction CarbonMonoxideOxidation { get; } = Create("oxidation.carbonMonoxide", "CO burns to CO₂", [(1, CarbonMonoxide), (0.5, Oxygen)], [(1, CarbonDioxide)]);

    public static Reaction MethaneCombustion { get; } = Create("combustion.methane.complete", "Methane burns completely", [(1, Methane), (2, Oxygen)], [(1, CarbonDioxide), (2, WaterVapour)]);

    public static Reaction MethaneCombustionGross { get; } = Create("combustion.methane.complete.gross", "Methane burns completely, water condensed", [(1, Methane), (2, Oxygen)], [(1, CarbonDioxide), (2, LiquidWater)]);

    public static Reaction MethaneToCarbonMonoxide { get; } = Create("combustion.methane.carbonMonoxide", "Methane burns to CO", [(1, Methane), (1.5, Oxygen)], [(1, CarbonMonoxide), (2, WaterVapour)]);

    public static Reaction MethaneToSoot { get; } = Create("combustion.methane.soot", "Methane burns to soot", [(1, Methane), (1, Oxygen)], [(1, Carbon), (2, WaterVapour)]);

    public static Reaction OctaneCombustion { get; } = Create("combustion.octane.complete", "Octane burns completely", [(1, Octane), (12.5, Oxygen)], [(8, CarbonDioxide), (9, WaterVapour)]);

    public static Reaction SulfurCombustion { get; } = Create("combustion.sulfur", "Sulfur burns to SO₂", [(1, Sulfur), (1, Oxygen)], [(1, SulfurDioxide)]);

    public static Reaction Calcination { get; } = Create("calcination.calciumCarbonate", "Limestone calcination", [(1, CalciumCarbonate)], [(1, CalciumOxide), (1, CarbonDioxide)], "heat");

    public static Reaction ThermalNitricOxide { get; } = Create("nox.thermal", "Thermal NO formation", [(1, Nitrogen), (1, Oxygen)], [(2, NitricOxide)], "flame temperature");

    public static Reaction NitricOxideOxidation { get; } = Create("nox.oxidation", "NO oxidises to NO₂", [(2, NitricOxide), (1, Oxygen)], [(2, NitrogenDioxide)]);

    public static Reaction SteamMethaneReforming { get; } = Create("reforming.steamMethane", "Steam methane reforming", [(1, Methane), (2, WaterVapour)], [(1, CarbonDioxide), (4, Hydrogen)], "heat");

    public static Reaction IronOreReduction { get; } = Create("reduction.ironOre", "Iron-ore reduction", [(1, IronOxide), (3, CarbonMonoxide)], [(2, Iron), (3, CarbonDioxide)]);

    public static Reaction Recarbonation { get; } = Create("recarbonation.calciumOxide", "Recarbonation", [(1, CalciumOxide), (1, CarbonDioxide)], [(1, CalciumCarbonate)]);

    public static Reaction LimestoneDesulfurisation { get; } = Create("desulfurisation.limestone", "Flue-gas desulfurisation with limestone", [(1, CalciumCarbonate), (1, SulfurDioxide), (0.5, Oxygen)], [(1, CalciumSulfate), (1, CarbonDioxide)]);

    // Zeldovich NO formation steps
    public static Reaction ZeldovichOxygenAtom { get; } = Create("zeldovich.oxygenAtom", "O atom attacks N₂", [(1, OxygenAtom), (1, Nitrogen)], [(1, NitricOxide), (1, NitrogenAtom)], "flame temperature");

    public static Reaction ZeldovichNitrogenAtom { get; } = Create("zeldovich.nitrogenAtom", "N atom takes O from O₂", [(1, NitrogenAtom), (1, Oxygen)], [(1, NitricOxide), (1, OxygenAtom)], "flame temperature");

    // CO → ozone steps
    public static Reaction CarbonMonoxideHydroxyl { get; } = Create("ozone.carbonMonoxideHydroxyl", "OH oxidises CO", [(1, CarbonMonoxide), (1, Hydroxyl)], [(1, CarbonDioxide), (1, HydrogenAtom)]);

    public static Reaction HydroperoxylFormation { get; } = Create("ozone.hydroperoxylFormation", "H joins O₂", [(1, HydrogenAtom), (1, Oxygen)], [(1, Hydroperoxyl)], "+M");

    public static Reaction HydroperoxylNitricOxide { get; } = Create("ozone.hydroperoxylNitricOxide", "HO₂ turns NO into NO₂", [(1, Hydroperoxyl), (1, NitricOxide)], [(1, Hydroxyl), (1, NitrogenDioxide)]);

    public static Reaction NitrogenDioxidePhotolysis { get; } = Create("ozone.nitrogenDioxidePhotolysis", "Sunlight splits NO₂", [(1, NitrogenDioxide)], [(1, NitricOxide), (1, OxygenAtom)], "sunlight");

    public static Reaction OzoneFormation { get; } = Create("ozone.formation", "O joins O₂ to make ozone", [(1, OxygenAtom), (1, Oxygen)], [(1, Ozone)], "+M");

    public static Reaction CarbonMonoxideToOzone { get; } = Create("ozone.net", "CO makes ozone (net)", [(1, CarbonMonoxide), (2, Oxygen)], [(1, CarbonDioxide), (1, Ozone)], "sunlight, NOx");

    // SO₂ → sulfate steps
    public static Reaction SulfurDioxideHydroxyl { get; } = Create("sulfate.sulfurDioxideHydroxyl", "OH adds to SO₂", [(1, SulfurDioxide), (1, Hydroxyl)], [(1, Hydroxysulfonyl)], "+M");

    public static Reaction HydroxysulfonylOxygen { get; } = Create("sulfate.hydroxysulfonylOxygen", "HOSO₂ gives SO₃", [(1, Hydroxysulfonyl), (1, Oxygen)], [(1, Hydroperoxyl), (1, SulfurTrioxide)]);

    public static Reaction SulfurTrioxideHydration { get; } = Create("sulfate.sulfurTrioxideHydration", "SO₃ takes up water", [(1, SulfurTrioxide), (1, WaterVapour)], [(1, SulfuricAcid)]);

    public static Reaction SulfateGasPhase { get; } = Create("sulfate.gasPhase.net", "SO₂ to sulfate, gas phase (net)", [(1, SulfurDioxide), (1, Hydroxyl), (1, Oxygen), (1, WaterVapour)], [(1, SulfuricAcid), (1, Hydroperoxyl)]);

    public static Reaction SulfateAqueous { get; } = Create("sulfate.aqueous", "SO₂ to sulfate in cloud", [(1, SulfurDioxide), (1, HydrogenPeroxide)], [(1, SulfuricAcid)], "in cloud");

    public static IReadOnlyList<Reaction> All { get; } =
    [
        CarbonCombustion,
        CarbonToCarbonMonoxide,
        CarbonMonoxideOxidation,
        MethaneCombustion,
        MethaneCombustionGross,
        MethaneToCarbonMonoxide,
        MethaneToSoot,
        OctaneCombustion,
        SulfurCombustion,
        Calcination,
        ThermalNitricOxide,
        NitricOxideOxidation,
        SteamMethaneReforming,
        IronOreReduction,
        Recarbonation,
        LimestoneDesulfurisation,
        ZeldovichOxygenAtom,
        ZeldovichNitrogenAtom,
        CarbonMonoxideHydroxyl,
        HydroperoxylFormation,
        HydroperoxylNitricOxide,
        NitrogenDioxidePhotolysis,
        OzoneFormation,
        CarbonMonoxideToOzone,
        SulfurDioxideHydroxyl,
        HydroxysulfonylOxygen,
        SulfurTrioxideHydration,
        SulfateGasPhase,
        SulfateAqueous,
    ];

    private static Reaction Create(
        string id,
        string name,
        IReadOnlyList<(double Coefficient, Species Species)> reactants,
        IReadOnlyList<(double Coefficient, Species Species)> products,
        string? conditions = null) =>
        new(
            id,
            name,
            reactants.Select(term => new ReactionTerm(term.Coefficient, term.Species)).ToList(),
            products.Select(term => new ReactionTerm(term.Coefficient, term.Species)).ToList(),
            conditions);
}
