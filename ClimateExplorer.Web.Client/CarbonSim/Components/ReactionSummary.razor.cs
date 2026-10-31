namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The burner's equation, how much it has used and made so far, and the heat released. A scenario adds its own totals
/// as <see cref="ChildContent"/>: <c>&lt;div&gt;</c>s of <c>&lt;dt&gt;</c> and <c>&lt;dd&gt;</c>.
/// </summary>
public partial class ReactionSummary
{
    [Parameter]
    [EditorRequired]
    public SimulationState State { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public Reaction Reaction { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private string EquationAriaLabel => $"Equation: {Reaction.Name}, {Reaction.ToEquationString()}";

    private string MassUnitText => Units.ShortLabel(UnitOfMeasure.Kilograms);

    private IEnumerable<Row> Rows =>
    [
        Row.For("Reactants", true, Reaction.Reactants[0].Species, State.FuelBurnt),
        Row.For("Reactants", false, SpeciesCatalog.Oxygen, State.OxygenUsed),
        Row.For("Products", true, SpeciesCatalog.CarbonDioxide, State.CarbonDioxideMade),
        Row.For("Products", false, SpeciesCatalog.WaterVapour, State.WaterMade),
    ];

    private string MassText(double kg) => CarbonSimFormat.Number(Units.ToDisplay(Measure.Level(kg, UnitOfMeasure.Kilograms)));

    private sealed record Row(string Group, bool FirstOfGroup, string Formula, string Name, double Moles, double MassKg)
    {
        public static Row For(string group, bool firstOfGroup, Species species, double moles) =>
            new(group, firstOfGroup, species.ToDisplayString(), species.Name, moles, moles * species.MolarMass / 1000);
    }
}
