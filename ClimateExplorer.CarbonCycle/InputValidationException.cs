namespace ClimateExplorer.CarbonCycle;

// Thrown when a reader's input is out of range. Each error names the field, so the UI can show it there.
public sealed class InputValidationException : ArgumentException
{
    public InputValidationException(IReadOnlyList<ValidationError> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }

    public InputValidationException(string field, string message)
        : this([new ValidationError(field, message)])
    {
    }

    public IReadOnlyList<ValidationError> Errors { get; }

    internal static void ThrowIfAny(List<ValidationError> errors)
    {
        if (errors.Count > 0)
        {
            throw new InputValidationException(errors);
        }
    }
}
