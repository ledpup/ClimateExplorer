namespace ClimateExplorer.Web.Client.Components.Info;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class PrecipitationAnomalyInfo
{
    private const UnitOfMeasure Unit = UnitOfMeasure.Millimetres;

    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    public CalculatedAnomaly? CalculatedAnomaly { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private string AnomalyAsString => CalculatedAnomaly is null
        ? "NA"
        : Units.Format(ApiMeasures.Anomaly(CalculatedAnomaly, Unit), signed: true);

    private int AnomalyDecimals => Units.Decimals(Unit);

    private string FormatAverage(double average) => Units.Format(ApiMeasures.AnomalyPeriodAverage(average, Unit));
}
