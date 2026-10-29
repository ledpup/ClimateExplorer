namespace ClimateExplorer.CarbonCycle.Calculation;

public enum Material
{
    Fuel,

    // O₂ taken from the air
    Oxygen,

    CalciumCarbonate,
    CalciumOxide,
    CarbonDioxide,

    // Formed from the fuel's hydrogen, plus its moisture, as vapour
    Water,

    CarbonMonoxide,
    Soot,
    UnburntMethane,
    Nmvoc,
    SulfurDioxide,

    // Reported as NO₂
    Nox,

    // N₂ from the fuel's own nitrogen
    Nitrogen,

    Ash,
}
