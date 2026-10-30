namespace ClimateExplorer.CarbonCycle.Calculation;

// Mapping onto EmissionResult is Tab 1 step 8.
public sealed class Tab1ChemistryResult
{
    public Tab1ChemistryResult(IReadOnlyList<Tab1Stream> streams)
    {
        Streams = streams;
        Totals = new Tab1Stream(
            Tab1StreamKind.Total,
            Sum(streams.SelectMany(stream => stream.Reactants)),
            Sum(streams.SelectMany(stream => stream.Products)),
            streams.Sum(stream => stream.EnthalpyChange),
            streams.Aggregate(CarbonByFate.None, (total, stream) => total + stream.CarbonByFate),
            streams.SelectMany(stream => stream.Reactions).ToList());
    }

    // Combustion, then calcination (rock) if there is one
    public IReadOnlyList<Tab1Stream> Streams { get; }

    public Tab1Stream Totals { get; }

    public Tab1Stream? Stream(Tab1StreamKind kind) => Streams.FirstOrDefault(stream => stream.Kind == kind);

    private static List<MaterialAmount> Sum(IEnumerable<MaterialAmount> amounts) =>
        amounts
            .GroupBy(amount => amount.Material)
            .Select(group => new MaterialAmount(
                group.Key,
                group.Sum(amount => amount.MassKg),
                group.All(amount => amount.Moles is not null) ? group.Sum(amount => amount.Moles!.Value) : null))
            .ToList();
}
