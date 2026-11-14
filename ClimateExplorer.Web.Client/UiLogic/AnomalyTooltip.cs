namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The hover tooltip of an anomaly overview field (warming, precipitation): what the anomaly means and,
/// once the anomaly is calculated, the two period averages it's the difference of.
/// </summary>
public static class AnomalyTooltip
{
    public static string Build(string description, CalculatedAnomaly? anomaly, UnitOfMeasure unit, IUnitFormatter units)
    {
        var periods = anomaly is null
            ? string.Empty
            : "<ul>"
                + Period(anomaly.FirstYearInLast30Years, anomaly.LastYearInLast30Years, anomaly.AverageOfLast30Years, unit, units)
                + Period(anomaly.FirstYearInFirstHalf, anomaly.LastYearInFirstHalf, anomaly.AverageOfFirstHalf, unit, units)
                + "</ul>";

        return $"<p>{description}</p>{periods}<p>Click for more information.</p>";
    }

    // One place more than the anomaly, as in the info popup, so the rounded averages subtract to the anomaly shown.
    private static string Period(int firstYear, int lastYear, double average, UnitOfMeasure unit, IUnitFormatter units)
    {
        return $"<li>{firstYear}–{lastYear} average: {units.Format(ApiMeasures.AnomalyPeriodAverage(average, unit), extraDecimals: 1)}</li>";
    }
}
