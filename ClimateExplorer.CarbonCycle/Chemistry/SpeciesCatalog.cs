namespace ClimateExplorer.CarbonCycle.Chemistry;

// Only what the Tab 1 plan and its combustor sub-plan use (engine plan §4.5). Enthalpies are provisional.
public static class SpeciesCatalog
{
    public static Species Carbon { get; } = new("C(s,graphite)", "Carbon (graphite)", "C", Phase.Solid, 0, Sources.ProvisionalEnthalpy);

    public static Species Hydrogen { get; } = new("H2(g)", "Hydrogen", "H2", Phase.Gas, 0, Sources.ProvisionalEnthalpy);

    public static Species Oxygen { get; } = new("O2(g)", "Oxygen", "O2", Phase.Gas, 0, Sources.ProvisionalEnthalpy);

    public static Species Nitrogen { get; } = new("N2(g)", "Nitrogen", "N2", Phase.Gas, 0, Sources.ProvisionalEnthalpy);

    public static Species Sulfur { get; } = new("S(s,rhombic)", "Sulfur", "S", Phase.Solid, 0, Sources.ProvisionalEnthalpy);

    public static Species Iron { get; } = new("Fe(s)", "Iron", "Fe", Phase.Solid, 0, Sources.ProvisionalEnthalpy);

    public static Species CarbonDioxide { get; } = new("CO2(g)", "Carbon dioxide", "CO2", Phase.Gas, -393.5, Sources.ProvisionalEnthalpy);

    public static Species CarbonMonoxide { get; } = new("CO(g)", "Carbon monoxide", "CO", Phase.Gas, -110.5, Sources.ProvisionalEnthalpy);

    // Net heating value basis
    public static Species WaterVapour { get; } = new("H2O(g)", "Water vapour", "H2O", Phase.Gas, -241.8, Sources.ProvisionalEnthalpy);

    // Gross heating value comparison; moisture in real fuels
    public static Species LiquidWater { get; } = new("H2O(l)", "Water", "H2O", Phase.Liquid, -285.8, Sources.ProvisionalEnthalpy);

    public static Species Methane { get; } = new("CH4(g)", "Methane", "CH4", Phase.Gas, -74.9, Sources.ProvisionalEnthalpy);

    public static Species Ethane { get; } = new("C2H6(g)", "Ethane", "C2H6", Phase.Gas, -84.0, Sources.ProvisionalEnthalpy);

    public static Species Propane { get; } = new("C3H8(g)", "Propane", "C3H8", Phase.Gas, -104.7, Sources.ProvisionalEnthalpy);

    public static Species Butane { get; } = new("C4H10(g)", "Butane", "C4H10", Phase.Gas, -125.6, Sources.ProvisionalEnthalpy);

    public static Species Octane { get; } = new("C8H18(l)", "Octane", "C8H18", Phase.Liquid, -250.1, Sources.ProvisionalEnthalpy);

    // Stand-in for the aromatics in petrol and jet fuel
    public static Species Toluene { get; } = new("C7H8(l)", "Toluene", "C7H8", Phase.Liquid, 12.0, Sources.ProvisionalEnthalpy);

    public static Species Dodecane { get; } = new("C12H26(l)", "Dodecane", "C12H26", Phase.Liquid, -350.9, Sources.ProvisionalEnthalpy);

    public static Species Cetane { get; } = new("C16H34(l)", "Cetane (hexadecane)", "C16H34", Phase.Liquid, -456.1, Sources.ProvisionalEnthalpy);

    // Stand-in for the aromatics in diesel
    public static Species Methylnaphthalene { get; } = new("C11H10(l)", "1-Methylnaphthalene", "C11H10", Phase.Liquid, null, Sources.EnthalpyToSource);

    // Fast-cycle part of E10 petrol
    public static Species Ethanol { get; } = new("C2H5OH(l)", "Ethanol", "C2H5OH", Phase.Liquid, -277.6, Sources.ProvisionalEnthalpy);

    // Fast-cycle part of B7 diesel (biodiesel)
    public static Species MethylOleate { get; } = new("C19H36O2(l)", "Methyl oleate", "C19H36O2", Phase.Liquid, null, Sources.EnthalpyToSource);

    public static Species CalciumCarbonate { get; } = new("CaCO3(s,calcite)", "Calcium carbonate (calcite)", "CaCO3", Phase.Solid, -1207.6, Sources.ProvisionalEnthalpy);

    public static Species CalciumOxide { get; } = new("CaO(s)", "Calcium oxide (lime)", "CaO", Phase.Solid, -634.9, Sources.ProvisionalEnthalpy);

    public static Species IronOxide { get; } = new("Fe2O3(s)", "Iron(III) oxide", "Fe2O3", Phase.Solid, -824.2, Sources.ProvisionalEnthalpy);

    public static Species NitricOxide { get; } = new("NO(g)", "Nitric oxide", "NO", Phase.Gas, 90.3, Sources.ProvisionalEnthalpy);

    public static Species NitrogenDioxide { get; } = new("NO2(g)", "Nitrogen dioxide", "NO2", Phase.Gas, 33.2, Sources.ProvisionalEnthalpy);

    public static Species SulfurDioxide { get; } = new("SO2(g)", "Sulfur dioxide", "SO2", Phase.Gas, -296.8, Sources.ProvisionalEnthalpy);

    public static Species SulfurTrioxide { get; } = new("SO3(g)", "Sulfur trioxide", "SO3", Phase.Gas, -395.7, Sources.ProvisionalEnthalpy);

    public static Species Ozone { get; } = new("O3(g)", "Ozone", "O3", Phase.Gas, 142.7, Sources.ProvisionalEnthalpy);

    // Flue-gas desulfurisation (combustor sub-plan)
    public static Species CalciumSulfate { get; } = new("CaSO4(s)", "Calcium sulfate (gypsum, anhydrous)", "CaSO4", Phase.Solid, -1434.5, Sources.ProvisionalEnthalpy);

    public static Species SulfuricAcid { get; } = new("H2SO4(aq)", "Sulfuric acid", "H2SO4", Phase.Aqueous, null, Sources.DisplayOnly);

    public static Species HydrogenPeroxide { get; } = new("H2O2(aq)", "Hydrogen peroxide", "H2O2", Phase.Aqueous, null, Sources.DisplayOnly);

    // Radicals and atoms in mechanism steps; display only
    public static Species Hydroxyl { get; } = new("OH(g)", "Hydroxyl radical", "OH", Phase.Gas, null, Sources.DisplayOnly);

    public static Species HydrogenAtom { get; } = new("H(g)", "Hydrogen atom", "H", Phase.Gas, null, Sources.DisplayOnly);

    public static Species OxygenAtom { get; } = new("O(g)", "Oxygen atom", "O", Phase.Gas, null, Sources.DisplayOnly);

    public static Species NitrogenAtom { get; } = new("N(g)", "Nitrogen atom", "N", Phase.Gas, null, Sources.DisplayOnly);

    public static Species Hydroperoxyl { get; } = new("HO2(g)", "Hydroperoxyl radical", "HO2", Phase.Gas, null, Sources.DisplayOnly);

    public static Species Hydroxysulfonyl { get; } = new("HOSO2(g)", "Hydroxysulfonyl radical", "HOSO2", Phase.Gas, null, Sources.DisplayOnly);

    public static IReadOnlyList<Species> All { get; } =
    [
        Carbon,
        Hydrogen,
        Oxygen,
        Nitrogen,
        Sulfur,
        Iron,
        CarbonDioxide,
        CarbonMonoxide,
        WaterVapour,
        LiquidWater,
        Methane,
        Ethane,
        Propane,
        Butane,
        Octane,
        Toluene,
        Dodecane,
        Cetane,
        Methylnaphthalene,
        Ethanol,
        MethylOleate,
        CalciumCarbonate,
        CalciumOxide,
        IronOxide,
        NitricOxide,
        NitrogenDioxide,
        SulfurDioxide,
        SulfurTrioxide,
        Ozone,
        CalciumSulfate,
        SulfuricAcid,
        HydrogenPeroxide,
        Hydroxyl,
        HydrogenAtom,
        OxygenAtom,
        NitrogenAtom,
        Hydroperoxyl,
        Hydroxysulfonyl,
    ];
}
