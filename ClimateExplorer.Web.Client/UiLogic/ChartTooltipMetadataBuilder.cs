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
/// label (deliberately dropping adjustment/aggregation/smoothing detail that clutters the chart legend),
/// plus - for by-year series with enough history to satisfy
/// AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly - the last-30-years, full-period, and
/// early-period averages used to show how far the hovered value is from each. Series that don't qualify
/// for the anomaly figures still get a label; the tooltip just falls back to showing their plain value.
/// </summary>
public static class ChartTooltipMetadataBuilder
{
    public static List<ChartTooltipSeriesInfo> Build(IReadOnlyList<SeriesWithData> seriesWithData)
    {
        return [.. seriesWithData.Select(BuildForSeries)];
    }

    private static ChartTooltipSeriesInfo BuildForSeries(SeriesWithData series)
    {
        var dataSet = series.PreProcessedDataSet ?? series.SourceDataSet;
        var unitOfMeasure = dataSet.MeasurementDefinition!.UnitOfMeasure;

        return new ChartTooltipSeriesInfo
        {
            Label = series.ChartSeries!.GetTooltipLabel(unitOfMeasure),
            Rounding = UnitOfMeasureRounding(unitOfMeasure),
            Anomaly = BuildAnomaly(series, dataSet),
        };
    }

    private static ChartSeriesTooltipMetadata? BuildAnomaly(SeriesWithData series, DataSet dataSet)
    {
        if (series.ChartSeries!.BinGranularity != BinGranularities.ByYear)
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
