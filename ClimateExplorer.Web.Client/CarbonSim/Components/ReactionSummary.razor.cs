namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Simulation;
using Microsoft.AspNetCore.Components;

/// <summary>The burner's equation, and how much it has used and made so far.</summary>
public partial class ReactionSummary
{
    [Parameter]
    [EditorRequired]
    public HouseState State { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public Reaction Reaction { get; set; } = default!;

    private string EquationAriaLabel => $"Equation: {Reaction.Name}, {Reaction.ToEquationString()}";

    private double CondensedKg => State.LiquidWater * SpeciesCatalog.LiquidWater.MolarMass / 1000;

    private IEnumerable<Row> Rows =>
    [
        Row.For("Reactants", true, SpeciesCatalog.Methane, State.MethaneBurnt),
        Row.For("Reactants", false, SpeciesCatalog.Oxygen, State.OxygenUsed),
        Row.For("Products", true, SpeciesCatalog.CarbonDioxide, State.CarbonDioxideMade),
        Row.For("Products", false, SpeciesCatalog.WaterVapour, State.WaterMade),
    ];

    private sealed record Row(string Group, bool FirstOfGroup, string Formula, string Name, double Moles, double MassKg)
    {
        public static Row For(string group, bool firstOfGroup, Species species, double moles) =>
            new(group, firstOfGroup, species.ToDisplayString(), species.Name, moles, moles * species.MolarMass / 1000);
    }
}
