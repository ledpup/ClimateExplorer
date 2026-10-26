namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiModel;

/// <summary>
/// Turns a prepared chart (<c>ChartDataBuildResult</c>) into the Chart.js datasets and the matching
/// tooltip metadata. This is the chart's display boundary: everything before it is calculated in
/// metric, and this is where each series' values are converted, once, through its
/// <see cref="SeriesDisplayUnits"/>. Moved out of <c>ChartView</c> so it can be tested without a
/// component.
/// </summary>
public static class ChartDisplayBuilder
{
    /// <param name="seriesWithData">The rendered series, in chart order.</param>
    /// <param name="chartBins">The chart's x-axis bins, which run past the data when a trend is projected.</param>
    /// <param name="isMobileDevice">Whether to use the short legend labels and small points.</param>
    /// <param name="units">The reader's unit system.</param>
    public static ChartDisplayBuildResult BuildDatasets(
        IReadOnlyList<SeriesWithData> seriesWithData,
        BinIdentifier[] chartBins,
        bool isMobileDevice,
        IUnitFormatter units)
    {
        var colours = new ColourServer();

        var displayUnits = seriesWithData
            .Select(x => SeriesDisplayUnits.Resolve(x.ChartSeries!, x.ProcessedDataSet!.MeasurementDefinition!.UnitOfMeasure, units))
            .ToList();

        var labels = SeriesLabeller.Build(
            [.. seriesWithData.Select(x => x.ChartSeries!)],
            [.. displayUnits.Select(x => x.ShortLabel)],
            isMobileDevice);

        var datasets = new List<ChartJsDataset>();

        // Built in the same order as the datasets (real series first, then - once every real series is
        // in - the derived trend overlays below), so its indexes stay aligned with chart.js's dataset
        // indexes. That alignment is what lets the external tooltip look up a hovered trend point's
        // underlying series and show it against that series' own averages.
        var tooltipMetadata = new List<ChartTooltipSeriesInfo>();

        var requestedColours = seriesWithData
            .Where(x => x.ChartSeries!.RequestedColour != Colours.AutoAssigned)
            .Select(x => x.ChartSeries!.RequestedColour)
            .ToList();

        for (var i = 0; i < seriesWithData.Count; i++)
        {
            var chartSeries = seriesWithData[i];
            var dataSet = chartSeries.ProcessedDataSet!;
            var htmlColourCode = colours.GetNextColour(chartSeries.ChartSeries!.RequestedColour, requestedColours);
            var renderSmallPoints = isMobileDevice || dataSet.DataRecords.Count() > 400;

            datasets.Add(ChartLogic.GetSeriesChartDataset(
                chartSeries,
                dataSet,
                labels.Labels[i],
                htmlColourCode,
                renderSmallPoints: renderSmallPoints,
                displayUnits: displayUnits[i]));

            tooltipMetadata.Add(ChartTooltipMetadataBuilder.BuildForSeries(chartSeries, units));
        }

        // Trend projections are appended only once every real series has been added, because
        // ChartView.OnLineChartClicked treats any dataset index past the real series as a trend overlay.
        AddTrendDatasets(seriesWithData, chartBins, units, labels.TrendPrefixes, datasets, tooltipMetadata);

        return new ChartDisplayBuildResult(datasets, tooltipMetadata, labels.SeriesNote);
    }

    /// <summary>
    /// The chart legend label for one trend dataset. Unchanged from before multiple trends per
    /// series existed when a series has exactly one - "{parent} | {period} trend" - but folds in the
    /// regression type and predict-until year once a series has more than one, so two trends can
    /// never produce the same label even when they share a window (e.g. a linear and a quadratic fit
    /// over the same period, or the same fit projected to two different years).
    /// </summary>
    /// <param name="prefix">The parent's part of the label, from <see cref="SeriesLabels.TrendPrefixes"/>.</param>
    private static string BuildTrendLabel(string prefix, ChartSeriesTrend trend, bool hasMultipleTrends)
    {
        var period = TrendWindowLabel.Get(trend.Projection!.Window);

        return hasMultipleTrends
            ? $"{prefix} | {trend.RegressionType} {period} trend to {trend.Projection.LastYear}"
            : $"{prefix} | {period} trend";
    }

    private static string BuildTrendTooltipLabel(ChartSeriesDefinition csd, ChartSeriesTrend trend, bool hasMultipleTrends, string unitLabel)
    {
        var period = TrendWindowLabel.Get(trend.Projection!.Window);

        return hasMultipleTrends
            ? $"{csd.GetTooltipLabel(unitLabel)} | {trend.RegressionType} {period} trend"
            : $"{csd.GetTooltipLabel(unitLabel)} | {period} trend";
    }

    private static void AddTrendDatasets(
        IReadOnlyList<SeriesWithData> seriesWithData,
        BinIdentifier[] chartBins,
        IUnitFormatter units,
        IReadOnlyList<string> trendPrefixes,
        List<ChartJsDataset> datasets,
        List<ChartTooltipSeriesInfo> tooltipMetadata)
    {
        var binIndexesByYear = BuildBinIndexesByYear(chartBins);

        if (binIndexesByYear.Count == 0)
        {
            return;
        }

        for (var i = 0; i < seriesWithData.Count; i++)
        {
            var chartSeries = seriesWithData[i];
            var csd = chartSeries.ChartSeries!;
            var hasMultipleTrends = chartSeries.Trends.Count > 1;

            // The projection was fitted to the series' metric values, so it converts exactly as they do.
            var displayUnits = SeriesDisplayUnits.Resolve(csd, chartSeries.ProcessedDataSet!.MeasurementDefinition!.UnitOfMeasure, units);

            for (var tier = 0; tier < chartSeries.Trends.Count; tier++)
            {
                if (chartSeries.Trends[tier].Projection is not { } projection)
                {
                    continue;
                }

                var values = new List<double?>(new double?[chartBins.Length]);
                var plotted = false;

                foreach (var prediction in projection.Predictions)
                {
                    if (binIndexesByYear.TryGetValue((int)Math.Round(prediction.X), out var index))
                    {
                        values[index] = displayUnits.ToDisplay(prediction.PredictedY);
                        plotted = true;
                    }
                }

                if (!plotted)
                {
                    continue;
                }

                var trendColour = TrendSeriesColour.Derive(csd.Colour!, tier);
                var trendLabel = BuildTrendLabel(trendPrefixes[i], chartSeries.Trends[tier], hasMultipleTrends);

                datasets.Add(ChartLogic.GetTrendChartDataset(trendLabel, values, trendColour, displayUnits.AxisId));

                // The projection has no anomaly history of its own - it's compared against the real series
                // it was fitted to, so hovering a predicted point shows how far above/below that series'
                // last-30/full-period/early-period averages the prediction falls.
                tooltipMetadata.Add(ChartTooltipMetadataBuilder.BuildForTrendSeries(chartSeries, BuildTrendTooltipLabel(csd, chartSeries.Trends[tier], hasMultipleTrends, displayUnits.ShortLabel), units));
            }
        }
    }

    private static Dictionary<int, int> BuildBinIndexesByYear(BinIdentifier[] chartBins)
    {
        var binIndexesByYear = new Dictionary<int, int>();

        for (var i = 0; i < chartBins.Length; i++)
        {
            if (chartBins[i] is YearBinIdentifier yearBin)
            {
                binIndexesByYear[yearBin.Year] = i;
            }
        }

        return binIndexesByYear;
    }
}
