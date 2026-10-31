namespace ClimateExplorer.CarbonCycle.Calculation;

// kg of carbon, split by origin. Whether each part counts is the overview §2.3 rule, applied in Tab 1 step 8.
public sealed record CarbonAmount(double FossilKg, double FastCycleKg)
{
    public static CarbonAmount Zero { get; } = new(0, 0);

    public double TotalKg => FossilKg + FastCycleKg;

    public static CarbonAmount operator +(CarbonAmount a, CarbonAmount b) => new(a.FossilKg + b.FossilKg, a.FastCycleKg + b.FastCycleKg);

    public static CarbonAmount Split(double totalKg, double fastCycleShare) => new(totalKg * (1 - fastCycleShare), totalKg * fastCycleShare);
}
