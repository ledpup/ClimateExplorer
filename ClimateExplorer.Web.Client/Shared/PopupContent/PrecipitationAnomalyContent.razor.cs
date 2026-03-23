namespace ClimateExplorer.Web.Client.Shared.PopupContent;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using Microsoft.AspNetCore.Components;

public partial class PrecipitationAnomalyContent
{
    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    public CalculatedAnomaly? CalculatedAnomaly { get; set; }

    private string? AnomalyAsString { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AnomalyAsString = CalculatedAnomaly.ValueAsString("mm");
    }
}
