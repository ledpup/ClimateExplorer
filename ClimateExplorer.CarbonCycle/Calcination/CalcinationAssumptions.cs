namespace ClimateExplorer.CarbonCycle.Calcination;

// Defaults: Sources.ProvisionalCalcination
public sealed record CalcinationAssumptions(CalcinationProduct Clinker, CalcinationProduct Lime, double CementClinkerRatio)
{
    // Clinker ratio ~0.65–0.75; lowering it is a real-world lever, so it is editable.
    public static CalcinationAssumptions Default { get; } = new(CalcinationProduct.Clinker, CalcinationProduct.HighCalciumLime, 0.7);

    internal void Validate(List<ValidationError> errors)
    {
        Positive(errors, $"{Clinker.Id}.caOMassFraction", Clinker.CaOMassFraction);
        Positive(errors, $"{Lime.Id}.caOMassFraction", Lime.CaOMassFraction);
        Positive(errors, "cementClinkerRatio", CementClinkerRatio);
    }

    // A fraction above zero: a product with no CaO has nothing to calcine.
    private static void Positive(List<ValidationError> errors, string field, double fraction)
    {
        if (!double.IsFinite(fraction) || fraction <= 0 || fraction > 1)
        {
            errors.Add(new ValidationError(field, $"must be more than 0 and at most 1, not {fraction}."));
        }
    }
}
