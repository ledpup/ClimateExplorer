namespace ClimateExplorer.Web.Client.Components.Info;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class WarmingAnomalyInfo
{
    private const UnitOfMeasure Unit = UnitOfMeasure.DegreesCelsius;

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

    // One place more than the anomaly, so the worked example shows where the rounded difference comes from.
    private string FormatAverage(double average) => Units.Format(ApiMeasures.AnomalyPeriodAverage(average, Unit), extraDecimals: 1);
}
