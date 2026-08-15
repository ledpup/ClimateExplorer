namespace ClimateExplorer.Web.UiLogic;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Builds the per-series data the chart's external tooltip needs: a trimmed "Location | Data type | Unit"
/// label (deliberately dropping adjustment/aggregation/smoothing detail that clutters the chart legend) -
/// or, for transformations covered by ChartSeriesDefinition.GetTransformationOverrideLabel (e.g. Custom,
/// DayOfYearIfFrost), that same override label, so the tooltip always matches what the chart legend shows -
/// plus, for by-year series with enough history to satisfy
/// AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly, the last-30-years, full-period, and
/// early-period averages used to show how far the hovered value is from each. Series that don't qualify
/// for the anomaly figures still get a label; the tooltip just falls back to showing their plain value.
/// </summary>
public static class ChartTooltipMetadataBuilder
{
    public static List<ChartTooltipSeriesInfo> Build(IReadOnlyList<SeriesWithData> seriesWithData)
    {
        return [.. seriesWithData.Select(x => BuildForSeries(x))];
    }

    /// <summary>
    /// Builds tooltip metadata for a single real (non-trend) series. Exposed separately from
    /// <see cref="Build"/> so callers that build the chart's dataset list incrementally - interleaving
    /// real series with derived trend datasets, as ChartView does - can build each series' metadata at
    /// the point it's added, keeping it aligned with chart.js's dataset order.
    /// </summary>
    /// <param name="series">The series to build tooltip metadata for.</param>
    /// <returns>Tooltip metadata for <paramref name="series"/>, using its own tooltip label.</returns>
    public static ChartTooltipSeriesInfo BuildForSeries(SeriesWithData series)
    {
        return BuildForSeries(series, null);
    }

    /// <summary>
    /// Builds tooltip metadata for a trend/regression overlay dataset (a forward projection plotted
    /// as its own chart.js dataset - see ChartView.AddTrendDataSetsToChart). The projected values are
    /// still compared against the underlying series' own last-30/full-period/early-period averages, so
    /// hovering a prediction shows how far it sits from the real-data reference bands, not from itself.
    /// </summary>
    /// <param name="series">The real series the trend/regression was fitted to.</param>
    /// <param name="label">The tooltip row label for the trend dataset.</param>
    /// <returns>Tooltip metadata whose anomaly reference periods come from <paramref name="series"/>.</returns>
    public static ChartTooltipSeriesInfo BuildForTrendSeries(SeriesWithData series, string label)
    {
        return BuildForSeries(series, label);
    }

    private static ChartTooltipSeriesInfo BuildForSeries(SeriesWithData series, string? labelOverride = null)
    {
        var dataSet = series.PreProcessedDataSet ?? series.SourceDataSet;
        var unitOfMeasure = dataSet.MeasurementDefinition!.UnitOfMeasure;

        return new ChartTooltipSeriesInfo
        {
            Label = labelOverride ?? series.ChartSeries!.GetTooltipLabel(unitOfMeasure),
            Rounding = UnitOfMeasureRounding(unitOfMeasure),
            Anomaly = BuildAnomaly(series, dataSet),
        };
    }

    private static ChartSeriesTooltipMetadata? BuildAnomaly(SeriesWithData series, DataSet dataSet)
    {
        var chartSeries = series.ChartSeries;

        // Custom transformations (e.g. "x >= 25") turn the series into a boolean/threshold indicator,
        // for which a comparison against last-30/full-period/early-period averages isn't meaningful.
        // Fall back to the standard tooltip (plain value only) for these series.
        if (chartSeries.SeriesTransformation == SeriesTransformations.Custom)
        {
            return null;
        }

        if (chartSeries.BinGranularity != BinGranularities.ByYear)
        {
            return null;
        }

        // Use the series' full available history (not the chart's currently zoomed/filtered range), so the
        // reference periods don't shift as the user pans or zooms the chart.
        var anomaly = AnomalyCalculator.CalculateAnomaly(dataSet.DataRecords);

        if (anomaly == null)
        {
            return null;
        }

        return new ChartSeriesTooltipMetadata
        {
            Last30 = new ChartTooltipPeriod(anomaly.FirstYearInLast30Years, anomaly.LastYearInLast30Years, anomaly.MissingYearsInLast30Years, anomaly.AverageOfLast30Years),
            FullPeriod = new ChartTooltipPeriod(anomaly.FirstYearOverall, anomaly.LastYearOverall, anomaly.MissingYearsInFullPeriod, anomaly.AverageOfFullPeriod),
            Early = new ChartTooltipPeriod(anomaly.FirstYearInFirstHalf, anomaly.LastYearInFirstHalf, anomaly.MissingYearsInFirstHalf, anomaly.AverageOfFirstHalf),
        };
    }
}
