namespace ClimateExplorer.Web.Client.Components.Location.Info;

using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class ClimateStripeInfo
{
    private double min;
    private double max;

    private string? uomString;
    private int uomRounding;

    [Parameter]
    public string? LocationName { get; set; }

    [Parameter]
    public double? LocationMean { get; set; }

    [Parameter]
    public UnitOfMeasure? UnitOfMeasure { get; set; }

    [Parameter]
    public List<YearlyValues>? DataRecords { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (UnitOfMeasure == null)
        {
            return;
        }

        uomString = UnitOfMeasureLabelShort(UnitOfMeasure.Value);
        uomRounding = UnitOfMeasureRounding(UnitOfMeasure.Value);

        if (LocationMean == null)
        {
            return;
        }

        if (DataRecords != null)
        {
            min = DataRecords.Min(x => x.Relative);
            max = DataRecords.Max(x => x.Relative);
        }

        await base.OnParametersSetAsync();
    }
}
