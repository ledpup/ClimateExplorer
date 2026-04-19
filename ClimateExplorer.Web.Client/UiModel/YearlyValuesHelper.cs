namespace ClimateExplorer.Web.Client.UiModel;

using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public static class YearlyValuesHelper
{
    public static string GetTooltip(YearlyValues values, UnitOfMeasure unitOfMeasure, bool includeYear = true)
    {
        var uomString = UnitOfMeasureLabelShort(unitOfMeasure);
        var uomRounding = UnitOfMeasureRounding(unitOfMeasure);
        var aboveOrBelow = values.Relative > 0 ? "above" : "below";
        var title = includeYear ? $"Year {values.Year}<br>" : string.Empty;

        if (unitOfMeasure == UnitOfMeasure.Millimetres)
        {
            title += $"Precipitation total of {values.Absolute.ToString($"F{uomRounding}")}{uomString}<br>";
            title += $"{Math.Abs(values.Relative).ToString($"F{uomRounding}")}{uomString} {aboveOrBelow} average<br>";
            title += $"{Math.Round(values.PercentageOfAverage, 0)}% of the average";
        }
        else
        {
            title += $"Mean temperature of {values.Absolute.ToString($"F{uomRounding}")}{uomString}<br>";
            title += $"{Math.Abs(values.Relative).ToString($"F{uomRounding}")}{uomString} {aboveOrBelow} average";
        }

        return title;
    }
}
