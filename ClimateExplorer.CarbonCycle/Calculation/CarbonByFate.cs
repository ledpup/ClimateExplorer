namespace ClimateExplorer.CarbonCycle.Calculation;

// Where the carbon went. Rock carbon from calcination leaves as CO₂.
public sealed record CarbonByFate(CarbonAmount CarbonDioxide, CarbonAmount CarbonMonoxide, CarbonAmount Soot, CarbonAmount Unburnt)
{
    public static CarbonByFate None { get; } = new(CarbonAmount.Zero, CarbonAmount.Zero, CarbonAmount.Zero, CarbonAmount.Zero);

    public CarbonAmount Total => CarbonDioxide + CarbonMonoxide + Soot + Unburnt;

    public static CarbonByFate operator +(CarbonByFate a, CarbonByFate b) =>
        new(a.CarbonDioxide + b.CarbonDioxide, a.CarbonMonoxide + b.CarbonMonoxide, a.Soot + b.Soot, a.Unburnt + b.Unburnt);
}
