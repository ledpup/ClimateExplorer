namespace ClimateExplorer.CarbonCycle.Chemistry;

// Only the elements the Tab 1 plans use (engine plan §4.5).
public static class Elements
{
    public static Element H { get; } = new("H", "Hydrogen", 1, 1.008, Sources.AtomicWeights);

    public static Element C { get; } = new("C", "Carbon", 6, 12.011, Sources.AtomicWeights);

    public static Element N { get; } = new("N", "Nitrogen", 7, 14.007, Sources.AtomicWeights);

    public static Element O { get; } = new("O", "Oxygen", 8, 15.999, Sources.AtomicWeights);

    public static Element S { get; } = new("S", "Sulfur", 16, 32.06, Sources.AtomicWeights);

    public static Element Ca { get; } = new("Ca", "Calcium", 20, 40.078, Sources.AtomicWeights);

    public static Element Fe { get; } = new("Fe", "Iron", 26, 55.845, Sources.AtomicWeights);

    public static IReadOnlyList<Element> All { get; } = [H, C, N, O, S, Ca, Fe];

    public static Element FromSymbol(string symbol) =>
        All.FirstOrDefault(element => element.Symbol == symbol)
        ?? throw new ArgumentException($"Unknown element symbol '{symbol}'.", nameof(symbol));
}
