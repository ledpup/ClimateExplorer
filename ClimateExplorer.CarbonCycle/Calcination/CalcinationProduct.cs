namespace ClimateExplorer.CarbonCycle.Calcination;

// CaOMassFraction is editable. The rest of clinker (~a third) is mostly SiO₂, Al₂O₃ and Fe₂O₃ from clay, shale and sand,
// which are not carbonates and release no CO₂. MgO from dolomite is out of scope for now (engine plan §1).
public sealed record CalcinationProduct(string Id, string Name, double CaOMassFraction)
{
    // IPCC Tier 1: ~0.51–0.52 t CO₂ per t clinker
    public static CalcinationProduct Clinker { get; } = new("product.clinker", "Clinker", 0.65);

    // IPCC: ~0.75 t CO₂ per t high-calcium lime
    public static CalcinationProduct HighCalciumLime { get; } = new("product.lime.highCalcium", "High-calcium lime", 0.95);
}
