namespace ClimateExplorer.CarbonCycle;

internal static class InputValidation
{
    public static void NonNegative(List<ValidationError> errors, string field, double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            errors.Add(new ValidationError(field, $"must be zero or more, not {value}."));
        }
    }

    public static void Positive(List<ValidationError> errors, string field, double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            errors.Add(new ValidationError(field, $"must be more than zero, not {value}."));
        }
    }

    public static void Fraction(List<ValidationError> errors, string field, double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1)
        {
            errors.Add(new ValidationError(field, $"must be between 0 and 1, not {value}."));
        }
    }

    public static void SumsToOne(List<ValidationError> errors, string field, IEnumerable<double> values)
    {
        var sum = values.Sum();

        if (!Tolerances.AreClose(sum, 1))
        {
            errors.Add(new ValidationError(field, $"must sum to 1, not {sum}."));
        }
    }
}
