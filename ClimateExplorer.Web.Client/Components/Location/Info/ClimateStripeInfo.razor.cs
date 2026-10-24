namespace ClimateExplorer.Web.Client.Components.Location.Info;

using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class ClimateStripeInfo
{
    // The worked example's hypothetical anomaly, in the series' metric unit.
    private const double ExampleAnomaly = -1.3;

    private double min;
    private double max;

    [Parameter]
    public string? LocationName { get; set; }

    [Parameter]
    public double? LocationMean { get; set; }

    [Parameter]
    public UnitOfMeasure? UnitOfMeasure { get; set; }

    [Parameter]
    public List<YearlyValues>? DataRecords { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private string UnitLabel => Units.ShortLabel(UnitOfMeasure!.Value);

    protected override async Task OnParametersSetAsync()
    {
        if (UnitOfMeasure == null)
        {
            return;
        }

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

    private string FormatMean(double mean) => Units.Format(YearlyValuesHelper.LocationMean(mean, UnitOfMeasure!.Value));

    private string FormatAnomaly(double anomaly, bool signed = true) => Units.Format(Measure.Change(anomaly, UnitOfMeasure!.Value), signed);
}
