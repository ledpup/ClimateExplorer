namespace ClimateExplorer.CarbonCycle.Calcination;

using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;

// The rock stream of calcination (engine plan §6.3). Its heating fuel is a separate combustion stream.
public static class CalcinationCalculator
{
    public static Tab1Stream Calcine(RockMaterial material, double massKg, CalcinationAssumptions assumptions)
    {
        var errors = new List<ValidationError>();
        InputValidation.NonNegative(errors, "rockMass", massKg);
        assumptions.Validate(errors);
        InputValidationException.ThrowIfAny(errors);

        var reaction = ReactionCatalog.Calcination;
        var scaled = material switch
        {
            RockMaterial.Limestone => reaction.ScaleByMass(SpeciesCatalog.CalciumCarbonate, massKg),
            RockMaterial.Clinker => reaction.ScaleByMass(SpeciesCatalog.CalciumOxide, massKg * assumptions.Clinker.CaOMassFraction),
            RockMaterial.Cement => reaction.ScaleByMass(SpeciesCatalog.CalciumOxide, massKg * assumptions.CementClinkerRatio * assumptions.Clinker.CaOMassFraction),
            RockMaterial.Lime => reaction.ScaleByMass(SpeciesCatalog.CalciumOxide, massKg * assumptions.Lime.CaOMassFraction),
            _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown rock material."),
        };

        var calciumCarbonate = scaled.Of(SpeciesCatalog.CalciumCarbonate);
        var calciumOxide = scaled.Of(SpeciesCatalog.CalciumOxide);
        var carbonDioxide = scaled.Of(SpeciesCatalog.CarbonDioxide);
        var rockCarbonKg = (carbonDioxide.Moles * Elements.C.StandardAtomicWeight) / 1000;

        return new Tab1Stream(
            Tab1StreamKind.Calcination,
            [new(Material.CalciumCarbonate, calciumCarbonate.MassKg, calciumCarbonate.Moles)],
            [new(Material.CalciumOxide, calciumOxide.MassKg, calciumOxide.Moles), new(Material.CarbonDioxide, carbonDioxide.MassKg, carbonDioxide.Moles)],
            scaled.EnthalpyChange!.Value,
            CarbonByFate.None with { CarbonDioxide = new CarbonAmount(rockCarbonKg, 0) },
            [reaction]);
    }
}
