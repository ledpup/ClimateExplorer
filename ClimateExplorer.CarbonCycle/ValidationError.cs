namespace ClimateExplorer.CarbonCycle;

// Field is the input's name as the UI knows it, e.g. "fuelMass", "carbonSplit", "fuel.components".
public sealed record ValidationError(string Field, string Message)
{
    public override string ToString() => $"{Field}: {Message}";
}
