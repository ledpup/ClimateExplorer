namespace ClimateExplorer.Web.Client.UiModel;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The display boundary for a climate stripe's <see cref="YearlyValues"/>: every number the stripe and its
/// info panel show is taken as a measure from here.
/// </summary>
public static class YearlyValuesHelper
{
    /// <summary>The year's mean temperature or rainfall total. A level.</summary>
    public static Measure Absolute(YearlyValues values, UnitOfMeasure unitOfMeasure) => Measure.Level(values.Absolute, unitOfMeasure);

    /// <summary>How far the year is above or below the series mean. A difference.</summary>
    public static Measure Relative(YearlyValues values, UnitOfMeasure unitOfMeasure) => Measure.Change(values.Relative, unitOfMeasure);

    /// <summary>The mean of the whole series. A level.</summary>
    public static Measure LocationMean(double mean, UnitOfMeasure unitOfMeasure) => Measure.Level(mean, unitOfMeasure);

    /// <summary>The year's place in the series, 1 being the warmest or wettest. Tied years share a rank.</summary>
    public static int Rank(YearlyValues values, IEnumerable<YearlyValues> records) => 1 + records.Count(x => x.Absolute > values.Absolute);

    /// <summary>E.g. "3rd warmest of 56 years".</summary>
    public static string GetRankDescription(int rank, int count, UnitOfMeasure unitOfMeasure)
    {
        var superlative = unitOfMeasure == UnitOfMeasure.Millimetres ? "wettest" : "warmest";
        return $"{RecentObservationComparison.FormatOrdinal(rank)} {superlative} of {count} years";
    }

    public static string GetTooltip(YearlyValues values, UnitOfMeasure unitOfMeasure, IUnitFormatter? units = null, bool includeYear = true)
    {
        units ??= UnitFormatter.Metric;

        var aboveOrBelow = values.Relative > 0 ? "above" : "below";
        var title = includeYear ? $"Year {values.Year}<br>" : string.Empty;
        var absolute = units.Format(Absolute(values, unitOfMeasure));

        // The distance from average without its sign, since "above" or "below" says which way.
        var distance = units.Format(Measure.Change(Math.Abs(values.Relative), unitOfMeasure));

        if (unitOfMeasure == UnitOfMeasure.Millimetres)
        {
            title += $"Precipitation total of {absolute}<br>";
            title += $"{distance} {aboveOrBelow} average<br>";
            title += $"{Math.Round(values.PercentageOfAverage, 0)}% of the average";
        }
        else
        {
            title += $"Mean temperature of {absolute}<br>";
            title += $"{distance} {aboveOrBelow} average";
        }

        return title;
    }
}
