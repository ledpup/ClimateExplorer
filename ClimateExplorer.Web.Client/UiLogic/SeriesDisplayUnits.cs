namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The units one chart series is shown in, resolved once per series. Every number and label for the
/// series - plotted values, trend projections, axis range and title, legend, tooltip and CSV - goes
/// through the same instance, so a converted number can never get a metric label or the reverse.
/// </summary>
/// <remarks>
/// A series that can't be converted with certainty is shown in metric with its metric label, whatever
/// unit system the reader chose. See <see cref="IsConvertible"/>.
/// </remarks>
public sealed class SeriesDisplayUnits
{
    private SeriesDisplayUnits(UnitOfMeasure unit, MeasureKind kind, IUnitFormatter formatter, string axisId)
    {
        Unit = unit;
        Kind = kind;
        Formatter = formatter;
        AxisId = axisId;
    }

    /// <summary>The metric unit the series' values are calculated in.</summary>
    public UnitOfMeasure Unit { get; }

    public MeasureKind Kind { get; }

    /// <summary>
    /// The formatter this series is actually shown with: the reader's, or metric when the series can't
    /// be converted.
    /// </summary>
    public IUnitFormatter Formatter { get; }

    public string AxisId { get; }

    public string ShortLabel => Formatter.ShortLabel(Unit);

    public int Decimals => Formatter.Decimals(Unit);

    public static SeriesDisplayUnits Resolve(ChartSeriesDefinition series, UnitOfMeasure unit, IUnitFormatter units)
    {
        var formatter = IsConvertible(series, unit) ? units : UnitFormatter.Metric;
        var axisId = ChartLogic.GetYAxisId(series.SeriesTransformation, series.CustomTransformation, unit, series.Aggregation);

        // A series left in metric must not share an axis with converted series of the same unit, or
        // °C and °F values would be read against one scale. The suffix only appears in that case, so
        // axis ids - which ChartState keys scale-to-zero settings by - are otherwise unchanged.
        if (units.Converts(unit) && !formatter.Converts(unit))
        {
            axisId += "Metric";
        }

        return new SeriesDisplayUnits(unit, ResolveKind(series, unit), formatter, axisId);
    }

    /// <summary>
    /// Whether the series' values are levels (temperatures, totals) or differences between levels.
    /// </summary>
    public static MeasureKind ResolveKind(ChartSeriesDefinition series, UnitOfMeasure unit)
    {
        // The anomaly is calculated by the API (SeriesValueOptions.Anomaly) or the dataset is one of
        // anomalies; a difference series and an annual change subtract one level from another.
        var isDifference =
            series.Value == SeriesValueOptions.Anomaly
            || unit == UnitOfMeasure.DegreesCelsiusAnomaly
            || series.SeriesDerivationType is SeriesDerivationTypes.DifferenceBetweenTwoSeries or SeriesDerivationTypes.AverageOfAnomaliesInRegion
            || series.TemporalCalculation == TemporalCalculationOptions.AnnualChange;

        return isDifference ? MeasureKind.Change : MeasureKind.Level;
    }

    /// <summary>
    /// Whether the series' values can be converted correctly. They can't when:
    /// <list type="bullet">
    /// <item>a transformation other than <see cref="SeriesTransformations.Identity"/> has been applied -
    /// the values are counts, days of the year, flags or negated readings, not the source quantity;</item>
    /// <item>levels of °C have been summed (the <see cref="SeriesAggregationOptions.Sum"/> aggregation or a
    /// <see cref="TemporalCalculationOptions.Cumulative"/> total): the °F offset would be added once per
    /// summed value, and the number of values isn't known here.</item>
    /// </list>
    /// </summary>
    public static bool IsConvertible(ChartSeriesDefinition series, UnitOfMeasure unit)
    {
        if (series.SeriesTransformation != SeriesTransformations.Identity)
        {
            return false;
        }

        var sumsLevels =
            ResolveKind(series, unit) == MeasureKind.Level
            && (series.Aggregation == SeriesAggregationOptions.Sum || series.TemporalCalculation == TemporalCalculationOptions.Cumulative);

        return !(sumsLevels && unit == UnitOfMeasure.DegreesCelsius);
    }

    public Measure ToMeasure(double value)
    {
        return Kind == MeasureKind.Level ? Measure.Level(value, Unit) : Measure.Change(value, Unit);
    }

    public double? ToDisplay(double? value)
    {
        return value is { } v ? Formatter.ToDisplay(ToMeasure(v)) : null;
    }

    public double ToDisplay(double value)
    {
        return Formatter.ToDisplay(ToMeasure(value));
    }
}
