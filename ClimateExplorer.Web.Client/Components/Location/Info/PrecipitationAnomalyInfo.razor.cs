namespace ClimateExplorer.Web.Client.Components.Info;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class PrecipitationAnomalyInfo
{
    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    public CalculatedAnomaly? CalculatedAnomaly { get; set; }

    private string? AnomalyAsString { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AnomalyAsString = CalculatedAnomaly.ValueAsString(UnitOfMeasure.Millimetres);
    }
}
