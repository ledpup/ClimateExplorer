namespace ClimateExplorer.CarbonCycle.Calculation;

// Fractions of the fuel's carbon leaving as CO₂, CO, soot and unburnt fuel; they sum to 1.
public sealed record CarbonSplit(double CarbonDioxide, double CarbonMonoxide, double Soot, double Unburnt)
{
    // IPCC's oxidation factor of 1
    public static CarbonSplit Complete { get; } = new(1, 0, 0, 0);

    public double Burnt => 1 - Unburnt;

    internal void Validate(List<ValidationError> errors)
    {
        const string Field = "carbonSplit";

        InputValidation.Fraction(errors, Field + ".carbonDioxide", CarbonDioxide);
        InputValidation.Fraction(errors, Field + ".carbonMonoxide", CarbonMonoxide);
        InputValidation.Fraction(errors, Field + ".soot", Soot);
        InputValidation.Fraction(errors, Field + ".unburnt", Unburnt);
        InputValidation.SumsToOne(errors, Field, [CarbonDioxide, CarbonMonoxide, Soot, Unburnt]);
    }
}
