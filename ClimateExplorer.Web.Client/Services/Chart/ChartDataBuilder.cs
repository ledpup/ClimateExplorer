namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Fetches and prepares chart data. This is the Phase 5 extraction of the data-building
/// responsibilities that previously lived in <c>ChartView.RetrieveDataSets</c> and
/// <c>ChartView.BuildProcessedDataSets</c>. Behaviour is intended to match the original
/// in-component pipeline; user-facing warnings are returned in the result rather than raised
/// directly so the caller owns presentation.
/// </summary>
public sealed class ChartDataBuilder : IChartDataBuilder
{
    private readonly IDataService dataService;
    private readonly ILogger<ChartDataBuilder> logger;

    public ChartDataBuilder(IDataService dataService, ILogger<ChartDataBuilder> logger)
    {
        this.dataService = dataService;
        this.logger = logger;
    }

    public async Task<ChartDataBuildResult> BuildAsync(ChartState state, CancellationToken cancellationToken = default)
    {
        var usableSeries = state.Series.Where(x => x.DataAvailable).ToArray();

        if (usableSeries.Length == 0)
        {
            return new ChartDataBuildResult();
        }

        var binGranularity = usableSeries[0].BinGranularity;
        var internalGroupingThreshold = ResolveInternalGroupingThreshold(state.GroupingThresholdText);

        var seriesWithData = await RetrieveDataSets(usableSeries, binGranularity, state, internalGroupingThreshold);

        var messages = new List<UserNotification>();
        var processed = BuildProcessedDataSets(seriesWithData, binGranularity, state, messages);

        return processed with
        {
            Messages = messages,
            HasRenderableData = HasRenderableChartData(processed.ChartBins, processed.SeriesWithData),
        };
    }

    private static float ResolveInternalGroupingThreshold(string? groupingThresholdText)
    {
        return string.IsNullOrWhiteSpace(groupingThresholdText)
            ? 0.7f
            : float.Parse(groupingThresholdText) / 100;
    }

    private static ContainerAggregationFunctions MapSeriesAggregationOptionToBinAggregationFunction(SeriesAggregationOptions a)
    {
        return a switch
        {
            SeriesAggregationOptions.Mean => ContainerAggregationFunctions.Mean,
            SeriesAggregationOptions.Minimum => ContainerAggregationFunctions.Min,
            SeriesAggregationOptions.Maximum => ContainerAggregationFunctions.Max,
            SeriesAggregationOptions.Median => ContainerAggregationFunctions.Median,
            SeriesAggregationOptions.Sum => ContainerAggregationFunctions.Sum,
            _ => throw new NotImplementedException($"SeriesAggregationOptions {a}"),
        };
    }

    private static SeriesSpecification BuildDataPrepSeriesSpecification(SourceSeriesSpecification sss)
    {
        return
            new SeriesSpecification
            {
                DataSetDefinitionId = sss.DataSetDefinition!.Id,
                DataType = sss.MeasurementDefinition!.DataType,
                DataAdjustment = sss.MeasurementDefinition.DataAdjustment,
                DataResolution = sss.MeasurementDefinition.DataResolution,
                LocationId = sss.LocationId,
            };
    }

    private static float GetGroupingThreshold(float? groupingThreshold, bool userOverride, float internalGroupingThreshold, bool binGranularityIsLinear = false)
    {
        // If we're in linear time, all buckets in a bin must have passed the data completeness test.
        // Otherwise, we apply GroupingThreshold from either user input or ChartSeriesDefinition
        return binGranularityIsLinear
            ? 1.0f
            : (userOverride || groupingThreshold == null)
                ? internalGroupingThreshold
                : groupingThreshold.Value;
    }

    private static IReadOnlyList<short> GetStartYears(List<SeriesWithData> chartSeriesWithData)
    {
        var binGranularity = chartSeriesWithData.Select(x => x.ChartSeries!.BinGranularity).Distinct().Single();
        var dataSet = binGranularity == BinGranularities.ByYear ? chartSeriesWithData.Select(x => x.PreProcessedDataSet) : chartSeriesWithData.Select(x => x.SourceDataSet);

        // build a list of all the years in which data sets start, used by the UI to allow the user to conveniently select from them
        return [.. dataSet.Select(x => x!.GetStartYearForDataSet()).Distinct().OrderBy(x => x)];
    }

    private static UserNotification CreateCompletenessFilteringMessage(DataSet dataSet)
    {
        var dataType = dataSet.DataType.ToFriendlyName().ToLowerInvariant();
        var locationName = dataSet.GeographicalEntity?.ToString() ?? "the selected location";

        return new UserNotification
        {
            Message = $"{locationName}: the completeness threshold removed all <b>{dataType}</b> observations. There is not enough data to chart this series.",
            Type = NotificationType.Warning,
            LocationName = dataSet.GeographicalEntity?.ToString(),
        };
    }

    private static bool HasFiniteValue(DataSet? dataSet)
    {
        return dataSet?.DataRecords.Any(x => x.Value.HasValue && !double.IsNaN(x.Value.Value) && !double.IsInfinity(x.Value.Value)) == true;
    }

    /// <summary>
    /// Fits the three trend windows for every series with the trend module switched on, records why
    /// any window can't be offered, and returns the bin array extended to cover the furthest
    /// forward projection.
    /// </summary>
    private static BinIdentifier[] ApplyTrends(
        List<SeriesWithData> renderableChartSeries,
        BinIdentifier[] chartBins,
        BinGranularities binGranularity,
        List<UserNotification> messages)
    {
        var seriesWantingTrends = renderableChartSeries.Where(x => x.ChartSeries!.ShowTrend).ToList();

        if (seriesWantingTrends.Count == 0)
        {
            return chartBins;
        }

        // A trend regresses a value against a calendar year and projects one value per year, so it
        // only has meaning when the x-axis is years. Other granularities can still carry the
        // setting (from a shared URL, or from switching grouping with a trend active).
        if (binGranularity != BinGranularities.ByYear)
        {
            messages.Add(new UserNotification
            {
                Message = "Trends are only available when the chart is grouped by year, so no trend line was added.",
                Type = UiModel.NotificationType.Info,
            });

            return chartBins;
        }

        var binIdsToPlot = new HashSet<string>(chartBins.Select(x => x.Id));

        foreach (var cs in seriesWantingTrends)
        {
            var csd = cs.ChartSeries!;
            var subject = new TrendStatSubject(csd.GetFriendlyTitleShort(), ResolveTrendUnit(csd));

            var trend = ChartSeriesTrendCalculator.Calculate(
                subject,
                BuildTrendPoints(cs.PreProcessedDataSet!, binIdsToPlot),
                csd.TrendPeriod,
                csd.TrendPredictionYears);

            cs.Trend = trend;

            // The window that ended up being drawn is written back to the definition, so the
            // dropdown, the URL and the chart all agree - the user's request may have been for a
            // window that isn't significant for this data.
            csd.TrendPeriod = trend.Projection?.Window;

            var notification = ChartSeriesTrendNotificationBuilder.Build(
                subject.Label,
                trend,
                cs.SourceDataSet.GeographicalEntity?.Name,
                locationId: null);

            if (notification is not null)
            {
                messages.Add(notification);
            }
        }

        return ExtendBinsForProjections(chartBins, seriesWantingTrends);
    }

    private static BinIdentifier[] ExtendBinsForProjections(BinIdentifier[] chartBins, List<SeriesWithData> seriesWantingTrends)
    {
        var lastProjectedYear = seriesWantingTrends
            .Select(x => x.Trend?.Projection?.LastYear ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        if (chartBins.Length == 0 || chartBins[^1] is not YearBinIdentifier lastBin || lastProjectedYear <= lastBin.Year)
        {
            return chartBins;
        }

        var extraBins = Enumerable
            .Range(lastBin.Year + 1, lastProjectedYear - lastBin.Year)
            .Select(year => new YearBinIdentifier((short)year));

        return [.. chartBins, .. extraBins];
    }

    private static List<DataPoint> BuildTrendPoints(DataSet dataSet, HashSet<string> binIdsToPlot)
    {
        var points = new List<DataPoint>();

        foreach (var record in dataSet.DataRecords)
        {
            if (record.BinId is null
                || !binIdsToPlot.Contains(record.BinId)
                || !record.Value.HasValue
                || !double.IsFinite(record.Value.Value))
            {
                continue;
            }

            if (BinIdentifier.Parse(record.BinId) is YearBinIdentifier yearBin)
            {
                points.Add(new DataPoint(yearBin.Year, record.Value.Value));
            }
        }

        return points;
    }

    /// <summary>
    /// A transformed series is no longer in its source unit - "day of year if frost" is a day
    /// number, a custom expression is a count - so no unit is claimed rather than the wrong one.
    /// </summary>
    private static string ResolveTrendUnit(ChartSeriesDefinition csd)
    {
        if (csd.SeriesTransformation is not (SeriesTransformations.Identity or SeriesTransformations.Negate))
        {
            return string.Empty;
        }

        return UnitOfMeasureLabelShort(csd.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure);
    }

    private async Task<List<SeriesWithData>> RetrieveDataSets(
        IReadOnlyList<ChartSeriesDefinition> chartSeriesList,
        BinGranularities binGranularity,
        ChartState state,
        float internalGroupingThreshold)
    {
        var datasetsToReturn = new List<SeriesWithData>();

        logger.LogInformation("RetrieveDataSets: starting enumeration");

        List<Task<DataSet>> tasksToRun = [];

        foreach (var csd in chartSeriesList)
        {
            var cupAggregationFunction = MapSeriesAggregationOptionToBinAggregationFunction(csd.Aggregation);
            var bucketAggregationFunction = cupAggregationFunction;

            // If we're doing modular binning and the aggregation function is sum, then force mean aggregation at
            // top level
            var binAggregationFunction =
                binGranularity.IsModular() && cupAggregationFunction == ContainerAggregationFunctions.Sum
                ? ContainerAggregationFunctions.Mean
                : cupAggregationFunction;

            var dataSetTask =
                dataService.PostDataSet(
                    binGranularity,
                    binAggregationFunction,
                    bucketAggregationFunction,
                    cupAggregationFunction,
                    csd.Value,
                    csd.SourceSeriesSpecifications!.Select(BuildDataPrepSeriesSpecification).ToArray(),
                    csd.SeriesDerivationType,
                    GetGroupingThreshold(csd.GroupingThreshold, state.UserOverrideAggregationSettings, internalGroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold, state.UserOverrideAggregationSettings, internalGroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold, state.UserOverrideAggregationSettings, internalGroupingThreshold),
                    state.GroupingDays,
                    csd.SeriesTransformation,
                    csd.CustomTransformation,
                    csd.Year,
                    csd.MinimumDataResolution);

            tasksToRun.Add(dataSetTask);
        }

        datasetsToReturn.AddRange(
                await Task.WhenAll(
                    chartSeriesList.Select((series, i) =>
                        tasksToRun[i].ContinueWith(t => new SeriesWithData
                        {
                            ChartSeries = series,
                            SourceDataSet = t.Result,
                        }))));

        logger.LogInformation("RetrieveDataSets: completed enumeration");

        return datasetsToReturn;
    }

    private ChartDataBuildResult BuildProcessedDataSets(
        List<SeriesWithData> chartSeriesWithData,
        BinGranularities selectedBinGranularity,
        ChartState state,
        List<UserNotification> messages)
    {
        var l = new LogAugmenter(logger, "BuildProcessedDataSets");

        l.LogInformation("entering");

        foreach (var cs in chartSeriesWithData)
        {
            if (cs.ChartSeries!.SecondaryCalculation == SecondaryCalculationOptions.AnnualChange)
            {
                var yearlyDifferenceValues =
                    cs.SourceDataSet.DataRecords
                    .Select(x => x.Value)
                    .CalculateYearlyDifference();

                // Now, join back to the original DataRecord set
                var newDataRecords =
                    yearlyDifferenceValues
                    .Zip(
                        cs.SourceDataSet.DataRecords,
                        (val, dr) => new BinnedRecord(dr.BinId, val))
                    .ToList();

                cs.SourceDataSet =
                    new DataSet
                    {
                        GeographicalEntity = cs.SourceDataSet.GeographicalEntity,
                        MeasurementDefinition = cs.SourceDataSet.MeasurementDefinition,
                        DataRecords = newDataRecords,
                        SourceMetadata = cs.SourceDataSet.SourceMetadata,
                    };
            }
        }

        // If we're doing smoothing via the moving average, precalculate these data and add them to PreProcessedDataSets.
        // We do this because the SimpleMovingAverage calculate function will remove some years from the start of the data set.
        // It removes these years because it doesn't have a good enough average to present it to the user.
        // Therefore, we need to calculate the smoothing before we calculate the start year - the basis for labelling the chart
        // If we're not calculating a moving average, PreProcessedDataSets = SourceDataSets
        foreach (var cs in chartSeriesWithData)
        {
            if (!HasFiniteValue(cs.SourceDataSet))
            {
                cs.DataStatus = ChartSeriesDataStatus.NoChartableDataAfterCompletenessFiltering;
                cs.PreProcessedDataSet = cs.SourceDataSet;
                messages.Add(CreateCompletenessFilteringMessage(cs.SourceDataSet));
                continue;
            }

            // We only support moving averages on linear bin granularities (e.g. Year, YearAndMonth) - not modular ones like MonthOnly
            if (selectedBinGranularity.IsLinear() && cs.ChartSeries!.Smoothing == SeriesSmoothingOptions.MovingAverage)
            {
                var values =
                    cs.SourceDataSet.DataRecords
                    .Select(x => x.Value)
                    .CalculateCentredMovingAverage(cs.ChartSeries.SmoothingWindow, 0.75f);

                if (values.Count(y => y != null) < 10)
                {
                    messages.Add(new UserNotification
                    {
                        Message = $"{cs.SourceDataSet.GeographicalEntity?.Name}: not enough <b>{cs.SourceDataSet.DataType.ToFriendlyName().ToLower()}</b> data for a moving average. Showing unsmoothed data instead.",
                        Type = NotificationType.Warning,
                        LocationName = cs.SourceDataSet.GeographicalEntity?.Name,
                    });
                    cs.DataStatus = ChartSeriesDataStatus.FallbackToUnsmoothedData;
                    values = cs.SourceDataSet.DataRecords
                                            .Where(x => x.Value.HasValue)
                                            .Select(x => x.Value);
                }

                // Now, join back to the original DataRecord set
                var newDataRecords =
                    values
                    .Zip(
                        cs.SourceDataSet.DataRecords,
                        (val, dr) => new BinnedRecord(dr.BinId, val))
                    .ToList();

                cs.PreProcessedDataSet =
                    new DataSet
                    {
                        GeographicalEntity = cs.SourceDataSet.GeographicalEntity,
                        MeasurementDefinition = cs.SourceDataSet.MeasurementDefinition,
                        DataRecords = newDataRecords,
                        SourceMetadata = cs.SourceDataSet.SourceMetadata,
                    };
            }
            else
            {
                cs.PreProcessedDataSet = cs.SourceDataSet;
            }
        }

        var renderableChartSeries = chartSeriesWithData
            .Where(x => HasFiniteValue(x.PreProcessedDataSet))
            .ToList();

        if (renderableChartSeries.Count == 0)
        {
            l.LogWarning("No requested chart series produced finite values after preprocessing.");

            return new ChartDataBuildResult
            {
                SeriesWithData = [],
                NonRenderedSeriesWithData = chartSeriesWithData,
                StartYears = [],
            };
        }

        l.LogInformation("done with moving average calculation");

        // There must be exactly one bin granularity or else something odd's going on.
        var binGranularity = renderableChartSeries.Select(x => x.ChartSeries!.BinGranularity).Distinct().Single();

        if (binGranularity != selectedBinGranularity)
        {
            throw new Exception($"BinGranularity selected for series ({binGranularity}) doesn't match overall selected granularity {selectedBinGranularity}");
        }

        BinIdentifier? chartStartBin = null;
        BinIdentifier? chartEndBin = null;
        BinIdentifier[]? chartBins = null;
        IReadOnlyList<short> startYears = [];

        switch (binGranularity)
        {
            case BinGranularities.ByYear:
            case BinGranularities.ByYearAndMonth:
            case BinGranularities.ByYearAndWeek:
            case BinGranularities.ByYearAndDay:
                // Calculate first and last year which we have a data record for, across all data sets underpinning all chart series
                var preProcessedDataSets = renderableChartSeries.Select(x => x.PreProcessedDataSet);

                (chartStartBin, chartEndBin) =
                    ChartLogic.GetBinRangeToPlotForGaplessRange(
                        preProcessedDataSets!, // Pass in the data available for plotting
                        state.ChartAllData, // and the user's preferences about what x axis range they'd like plotted
                        state.StartYear!,
                        state.EndYear!);

                chartBins = BinHelpers.EnumerateBinsInRange(chartStartBin, chartEndBin).ToArray();

                startYears = GetStartYears(renderableChartSeries);

                // Trends are fitted over what is actually plotted, so this has to happen once the
                // bin range is known. It also has to happen before gap filling, because a trend
                // projection extends the bin array past the end of the data and the gap-filling
                // pass below is what gives every other series null records for those extra bins.
                chartBins = ApplyTrends(renderableChartSeries, chartBins, binGranularity, messages);

                break;

            case BinGranularities.ByMonthOnly:
            case BinGranularities.ByDayOnly:
            case BinGranularities.BySouthernHemisphereTemperateSeasonOnly:
            case BinGranularities.BySouthernHemisphereTropicalSeasonOnly:
                chartStartBin = null;
                chartEndBin = null;
                chartBins = BinHelpers.GetBinsForModularGranularity(binGranularity);
                break;

            default:
                throw new NotImplementedException($"binGranularity {binGranularity}");
        }

        foreach (var cs in renderableChartSeries)
        {
            l.LogInformation("constructing ProcessedDataSet");

            var recordsByBinId = cs.PreProcessedDataSet!.DataRecords.ToLookup(x => x.BinId);

            l.LogInformation("First chart bin: " + chartBins.First() + ", last chart: " + chartBins.Last());

            // Create new datasets, same as the source, but with any gaps filled with null records
            cs.ProcessedDataSet =
                new DataSet
                {
                    GeographicalEntity = cs.PreProcessedDataSet.GeographicalEntity,
                    MeasurementDefinition = cs.PreProcessedDataSet.MeasurementDefinition,
                    SourceMetadata = cs.PreProcessedDataSet.SourceMetadata,
                    DataRecords =
                        [.. chartBins
                        .Select(
                            bin =>

                            // If there's a record in the source dataset, use it
                            recordsByBinId[bin.Id].SingleOrDefault()

                            // Otherwise, create a null record
                            ?? new BinnedRecord(bin.Id, null))],
                };
        }

        // Now, we cut down the processed datasets to just the bins that we intend to display on the chart.
        // This should only affect linear (gapless) BinGranularities, but executes either way, in case we
        // later allow users to say "just give me month-ignoring-year, but only for months after 4 and before 7",
        // for example.
        var binIdsToPlot = new HashSet<string>(chartBins.Select(x => x.Id));
        foreach (var cswd in renderableChartSeries)
        {
            cswd.ProcessedDataSet!.DataRecords = [.. cswd.ProcessedDataSet.DataRecords.Where(x => binIdsToPlot.Contains(x.BinId!))];
        }

        l.LogInformation("leaving");

        return new ChartDataBuildResult
        {
            SeriesWithData = renderableChartSeries,
            NonRenderedSeriesWithData = [.. chartSeriesWithData.Except(renderableChartSeries)],
            ChartBins = chartBins,
            ChartStartBin = chartStartBin,
            ChartEndBin = chartEndBin,
            StartYears = startYears,
        };
    }

    private bool HasRenderableChartData(BinIdentifier[]? chartBins, IReadOnlyList<SeriesWithData> seriesWithData)
    {
        if (chartBins == null || chartBins.Length == 0)
        {
            logger.LogWarning("No chart bins were produced while preparing chart data.");
            return false;
        }

        if (seriesWithData.Count == 0)
        {
            logger.LogWarning("No chart series with data were produced while preparing chart data.");
            return false;
        }

        var renderablePointCount = seriesWithData
            .SelectMany(x => x.ProcessedDataSet?.DataRecords ?? Array.Empty<BinnedRecord>())
            .Count(x => x.Value.HasValue && !double.IsNaN(x.Value.Value) && !double.IsInfinity(x.Value.Value));

        if (renderablePointCount == 0)
        {
            logger.LogWarning("Processed chart datasets contain no finite non-null values.");
            return false;
        }

        return true;
    }
}
