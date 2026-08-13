namespace ClimateExplorer.Web.UiLogic;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.UiModel;

/// <summary>
/// Builds the per-series data the chart's external tooltip needs to show a value alongside its
/// deviation from the last-30-years, full-period, and early-period averages (see AnomalyCalculator).
/// Only meaningful for by-year series with enough history to satisfy
/// AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly - anything else yields a null entry, and
/// the tooltip falls back to showing the plain value for that series.
/// </summary>
public static class ChartTooltipMetadataBuilder
{
    public static List<ChartSeriesTooltipMetadata?> Build(IReadOnlyList<SeriesWithData> seriesWithData)
    {
        return [.. seriesWithData.Select(BuildForSeries)];
    }

    private static ChartSeriesTooltipMetadata? BuildForSeries(SeriesWithData series)
    {
        if (series.ChartSeries!.BinGranularity != BinGranularities.ByYear)
        {
            return null;
        }

        // Use the series' full available history (not the chart's currently zoomed/filtered range), so the
        // reference periods don't shift as the user pans or zooms the chart.
        var dataSet = series.PreProcessedDataSet ?? series.SourceDataSet;

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
