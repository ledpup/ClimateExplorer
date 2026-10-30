namespace ClimateExplorer.CarbonCycle.Calculation;

using ClimateExplorer.CarbonCycle.Calcination;
using ClimateExplorer.CarbonCycle.Fuels;

// The three stage 1 input modes (engine plan §6.4). Quantities are SI: kg and J. Volume inputs are converted with the
// fuel's density before they get here. A missing carbon split means complete combustion.
public static class Tab1ChemistryEngine
{
    // Mode 1: a quantity of fuel
    public static Tab1ChemistryResult BurnFuel(Fuel fuel, double fuelMassKg, CarbonSplit? split = null) =>
        new([CombustionCalculator.Burn(fuel, fuelMassKg, split ?? CarbonSplit.Complete)]);

    // Mode 2: the heat wanted (J released, positive). A smoky flame needs more fuel for the same heat.
    public static Tab1ChemistryResult SupplyHeat(Fuel fuel, double heatJoules, CarbonSplit? split = null)
    {
        var errors = new List<ValidationError>();
        InputValidation.NonNegative(errors, "heat", heatJoules);
        InputValidationException.ThrowIfAny(errors);

        var carbonSplit = split ?? CarbonSplit.Complete;

        return BurnFuel(fuel, FuelMassForHeat(fuel, heatJoules, carbonSplit), carbonSplit);
    }

    // Mode 3: limestone, clinker, cement or lime, plus the fuel that heats it. Stage 1 supplies exactly the reaction's
    // own heat, with no losses (Tab 1 §2.1).
    public static Tab1ChemistryResult CalcineRock(
        RockMaterial material,
        double massKg,
        Fuel heatingFuel,
        CalcinationAssumptions? assumptions = null,
        CarbonSplit? split = null)
    {
        var rock = CalcinationCalculator.Calcine(material, massKg, assumptions ?? CalcinationAssumptions.Default);
        var carbonSplit = split ?? CarbonSplit.Complete;
        var combustion = CombustionCalculator.Burn(heatingFuel, FuelMassForHeat(heatingFuel, rock.EnthalpyChange, carbonSplit), carbonSplit);

        return new([combustion, rock]);
    }

    // J released per kg of fuel at the split
    public static double HeatReleasedPerKg(Fuel fuel, CarbonSplit? split = null) =>
        -CombustionCalculator.Burn(fuel, 1, split ?? CarbonSplit.Complete).EnthalpyChange;

    private static double FuelMassForHeat(Fuel fuel, double heatJoules, CarbonSplit split)
    {
        var heatPerKg = HeatReleasedPerKg(fuel, split);

        if (heatPerKg <= 0)
        {
            throw new InputValidationException("carbonSplit", "at this carbon split the fuel releases no heat.");
        }

        return heatJoules / heatPerKg;
    }
}
