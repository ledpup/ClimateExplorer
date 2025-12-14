namespace ClimateExplorer.Web.Client.Shared;

using System.Dynamic;
using CurrentDevice;
using Blazorise;
using Blazorise.Charts;
using Blazorise.Charts.Trendline;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;
using System;
using ClimateExplorer.WebApiClient.Services;

public partial class ChartView
{
    private bool haveCalledResizeAtLeastOnce = false;

    private Chart<double?>? chart;
    private ChartTrendline<double?>? chartTrendline;

    private BinIdentifier? chartStartBin;
    private BinIdentifier? chartEndBin;

    private Modal? chartOptionsModal;
    private Modal? aggregationOptionsModal;

    private string? groupingThresholdText;

    public bool ChartLoadingIndicatorVisible { get; set; }
    public bool ChartLoadingErrored { get; set; }

    public BinGranularities SelectedBinGranularity { get; set; } = BinGranularities.ByYear;

    public List<SeriesWithData>? ChartSeriesWithData { get; set; }

    public bool ChartAllData { get; set; }
    public string? SelectedStartYear { get; set; }
    public string? SelectedEndYear { get; set; }
    public short SelectedGroupingDays { get; set; }
    public string? GroupingThresholdText
    {
        get
        {
            return groupingThresholdText;
        }
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                groupingThresholdText = value;
                InternalGroupingThreshold = float.Parse(value) / 100;
            }
        }
    }

    public List<ChartSeriesDefinition>? ChartSeriesList { get; set; } = new List<ChartSeriesDefinition>();

    [Parameter]
    public EventCallback BuildDataSetsEvent { get; set; }

    [Parameter]
    public EventCallback<DataDownloadPackage> DownloadDataEvent { get; set; }

    [Parameter]
    public EventCallback ShowAddDataSetModalEvent { get; set; }

    [Inject]
    private IDataService? DataService { get; set; }

    [Inject]
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    [Inject]
    private ILogger<Index>? Logger { get; set; }

    [Inject]
    private NavigationManager? NavManager { get; set; }

    private short SelectingGroupingDays { get; set; }

    private float InternalGroupingThreshold { get; set; } = .7f;

    private bool UserOverridePresetAggregationSettings { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    private BinIdentifier[]? ChartBins { get; set; }

    private bool? IsMobileDevice { get; set; }

    private bool? EnableRangeSlider { get; set; }
    private int SliderMin { get; set; }
    private int SliderMax { get; set; }
    private int? SliderStart { get; set; }
    private int? SliderEnd { get; set; }

    /// <summary>
    /// Gets or sets the chart type selected by the user on the options page.
    /// </summary>
    private ChartType SelectedChartType { get; set; }
    private List<short>? SelectedYears { get; set; }
    private List<short>? StartYears { get; set; }
    private short EndYear { get; set; }

    private ColourServer Colours { get; set; } = new ColourServer();

    private Modal? OptionsModal { get; set; }

    /// <summary>
    /// The chart type applied to the chart control. If any series is in "Bar" mode, we switch
    /// the entire chart to Bar type to ensure it renders, at the cost of a small misalignment
    /// between grid lines and datapoints for any line series that are being displayed.
    /// Otherwise, we display in "Line" mode to avoid that cost.
    /// </summary>
    private ChartType InternalChartType { get; set; }

    public async Task OnAddDataSet(DataSetLibraryEntry dle, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        Logger!.LogInformation("Adding dle " + dle.Name);

        ChartSeriesList =
            ChartSeriesList!
            .Concat(
                new List<ChartSeriesDefinition>()
                {
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = dle.SeriesDerivationType,
                        SourceSeriesSpecifications = dle.SourceSeriesSpecifications!.Select(x => BuildSourceSeriesSpecification(x, dataSetDefinitions)).ToArray(),
                        Aggregation = dle.SeriesAggregation,
                        BinGranularity = SelectedBinGranularity,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = null,
                    },
                })
            .ToList();

        await BuildDataSets();
    }

    public async Task OnChartPresetSelected(SuggestedChartPresetModel chartPresetModel)
    {
        if (chartPresetModel.ChartSeriesList == null || !chartPresetModel.ChartSeriesList.Any())
        {
            throw new Exception("No chart series definition passed as a parameter when preset was selected");
        }

        ChartAllData = chartPresetModel.ChartAllData;
        SelectedStartYear = chartPresetModel.StartYear?.ToString();
        SelectedEndYear = chartPresetModel.EndYear?.ToString();

        SelectedBinGranularity = chartPresetModel.ChartSeriesList.First().BinGranularity;

        ChartSeriesList = chartPresetModel.ChartSeriesList;

        await BuildDataSets();
    }

    public async Task<List<SeriesWithData>> RetrieveDataSets(IEnumerable<ChartSeriesDefinition> chartSeriesList)
    {
        var datasetsToReturn = new List<SeriesWithData>();

        Logger!.LogInformation("RetrieveDataSets: starting enumeration");

        foreach (var csd in chartSeriesList)
        {
            var cupAggregationFunction = MapSeriesAggregationOptionToBinAggregationFunction(csd.Aggregation);
            var bucketAggregationFunction = cupAggregationFunction;

            // If we're doing modular binning and the aggregation function is sum, then force mean aggregation at
            // top level
            var binAggregationFunction =
                SelectedBinGranularity.IsModular() && cupAggregationFunction == ContainerAggregationFunctions.Sum
                ? ContainerAggregationFunctions.Mean
                : cupAggregationFunction;

            DataSet dataSet =
                await DataService!.PostDataSet(
                    SelectedBinGranularity,
                    binAggregationFunction,
                    bucketAggregationFunction,
                    cupAggregationFunction,
                    csd.Value,
                    csd.SourceSeriesSpecifications!.Select(BuildDataPrepSeriesSpecification).ToArray(),
                    csd.SeriesDerivationType,
                    GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold),
                    SelectedGroupingDays,
                    csd.SeriesTransformation,
                    csd.Year,
                    csd.MinimumDataResolution);

            datasetsToReturn.Add(
                new SeriesWithData() { ChartSeries = csd, SourceDataSet = dataSet });
        }

        Logger!.LogInformation("RetrieveDataSets: completed enumeration");

        return datasetsToReturn;
    }

    public async Task HandleRedraw()
    {
        var l = new LogAugmenter(Logger!, "HandleRedraw");

        l.LogInformation("Entering");

        // This can happen at startup, or if the user switches off all data series
        if (ChartSeriesWithData == null || ChartSeriesWithData.Count == 0 || chart == null || chartTrendline == null)
        {
            if (chart != null)
            {
                await chart.Clear();
                await chart.SetOptionsObject(new { });
            }

            l.LogInformation("Bailing early as no chart data available");
            return;
        }

        await chart.Clear();

        var title = string.Empty;
        var subtitle = string.Empty;
        List<ChartTrendlineData>? trendlines = null;
        dynamic scales = new ExpandoObject();

        if (ChartLoadingErrored)
        {
            l.LogError("We have identified an error. Will not try to render the chart normally");
        }
        else
        {
            LogChartSeriesList();

            // We now set ChartType to Bar if any series is of type Bar, and Line otherwise.
            var newInternalChartType =
                ChartSeriesWithData.Any(x => x.ChartSeries!.DisplayStyle == SeriesDisplayStyle.Bar)
                ? ChartType.Bar
                : ChartType.Line;

            if (newInternalChartType != InternalChartType)
            {
                InternalChartType = newInternalChartType;

                await chart.ChangeType(newInternalChartType);
            }

            Colours = new ColourServer();

            // Data sets sometimes have internal gaps in data (i.e. years which have no data even though earlier
            // and later years have data). Additionally, they may have external gaps in data if the overall period
            // to be charted goes beyond the range of the available data in one particular data set.
            //
            // To ensure these gaps are handled correctly in the plotted chart, we build a new dataset that includes
            // records for each missing year. Value is set to null for those records.
            l.LogInformation("Calling BuildProcessedDataSets");

            BuildProcessedDataSets(ChartSeriesWithData, ChartAllData);

            title = ChartLogic.BuildChartTitle(ChartSeriesWithData);
            subtitle = ChartLogic.BuildChartSubtitle(chartStartBin, chartEndBin, SelectedBinGranularity, IsMobileDevice!.Value, SelectedGroupingDays, GetGroupingThresholdText());

            l.LogInformation("Calling AddDataSetsToGraph");

            trendlines = await AddDataSetsToChart();

            l.LogInformation("Trendlines count: " + trendlines.Count);

            l.LogInformation("Calling AddLabels");

            var labels = ChartBins!.Select(x => x.Label).ToArray();
            await chart.AddLabels(labels);

            scales = BuildChartScales();
        }

        object chartOptions = new
        {
            Animation = false,
            Responsive = true,
            MaintainAspectRatio = false,
            SpanGaps = false,
            Plugins = new
            {
                Title = new
                {
                    Text = title,
                    Display = true,
                    Color = "black",
                },
                Subtitle = new
                {
                    Text = subtitle,
                    Display = true,
                    Color = "black",
                },
                Tooltip = new
                {
                    Mode = IsMobileDevice!.Value ? "point" : "index",
                    Intersect = false,
                },
                Legend = new
                {
                    Position = "bottom",
                },
            },
            Scales = scales,
        };

        await chart.SetOptionsObject(chartOptions);

        if (trendlines != null && trendlines.Count > 0)
        {
            await chartTrendline.AddTrendLineOptions(trendlines);
        }

        await chart.Update();

        // The below line is required to get the chart.js component to honour the styling applied on the parent div
        // If you don't call resize, the chart will apply the styling only after you resize the window,
        // but it does not apply the style on the initial load of the page.
        // See https://www.chartjs.org/docs/latest/configuration/responsive.html for more information
        if (!haveCalledResizeAtLeastOnce)
        {
            await chart.Resize();
            haveCalledResizeAtLeastOnce = true;
        }

        ChartLoadingIndicatorVisible = false;
        StateHasChanged();

        l.LogInformation("Leaving");
    }

    public async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await OnSelectedBinGranularityChanged(BinGranularities.ByMonthOnly, false);

        var chartWithData = ChartSeriesWithData!
            .First(x =>
            (x.SourceDataSet!.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
            (x.SourceDataSet.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null));

        var chartSeries = ChartSeriesList!
            .First(x => x.SourceSeriesSpecifications!.Any(y =>
               (y.MeasurementDefinition!.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
               (y.MeasurementDefinition.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null)));

        ChartSeriesList =
            ChartSeriesList!
            .Concat(
                new List<ChartSeriesDefinition>()
                {
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = chartWithData.ChartSeries!.SourceSeriesSpecifications,
                        Aggregation = chartSeries.Aggregation,
                        BinGranularity = SelectedBinGranularity,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = yearAndDataTypeFilter.Year,
                    },
                })
            .ToList();

        await BuildDataSets();
    }

    internal void LoadingChart()
    {
        ChartLoadingIndicatorVisible = true;
        ChartLoadingErrored = false;
        LogChartSeriesList();
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        ChartLoadingIndicatorVisible = true;
        ChartLoadingErrored = false;

        SelectedYears = [];

        SliderMax = DateTime.Now.Year;
    }

    protected override void OnParametersSet()
    {
        ChartLoadingErrored = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsMobileDevice == null)
        {
            IsMobileDevice = await CurrentDeviceService!.Mobile();
        }
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

    private void LogChartSeriesList()
    {
        Logger!.LogInformation("ChartSeriesList: (SelectedBinGranularity is " + SelectedBinGranularity + ")");

        foreach (var csd in ChartSeriesList!)
        {
            Logger!.LogInformation("    " + csd.ToString());
        }
    }

    private async Task ChartAllDataToggle()
    {
        ChartAllData = !ChartAllData;
        await BuildDataSets();
    }

    private Task ShowChartOptionsInfo()
    {
        return chartOptionsModal!.Show();
    }

    private Task ShowAggregationOptionsInfo()
    {
        return aggregationOptionsModal!.Show();
    }

    private SourceSeriesSpecification BuildSourceSeriesSpecification(DataSetLibraryEntry.SourceSeriesSpecification sss, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        var dsd = dataSetDefinitions.Single(x => x.Id == sss.SourceDataSetId);

        var md = dsd.MeasurementDefinitions!.Single(x => x.DataType == sss.DataType && x.DataAdjustment == sss.DataAdjustment);

        return
            new SourceSeriesSpecification
            {
                LocationId = sss.LocationId,
                LocationName = sss.LocationName!,
                DataSetDefinition = dsd,
                MeasurementDefinition = md,
            };
    }

    private SeriesSpecification BuildDataPrepSeriesSpecification(SourceSeriesSpecification sss)
    {
        return
            new SeriesSpecification
            {
                DataSetDefinitionId = sss.DataSetDefinition!.Id,
                DataType = sss.MeasurementDefinition!.DataType,
                DataAdjustment = sss.MeasurementDefinition.DataAdjustment,
                LocationId = sss.LocationId,
            };
    }

    private float GetGroupingThreshold(float? groupingThreshold, bool binGranularityIsLinear = false)
    {
        // If we're in linear time, all buckets in a bin must have passed the data completeness test.
        // Otherwise, we apply GroupingThreshold from either user input or ChartSeriesDefinition
        return binGranularityIsLinear
            ? 1.0f
            : (UserOverridePresetAggregationSettings || groupingThreshold == null)
                ? InternalGroupingThreshold
                : groupingThreshold.Value;
    }

    private async Task<List<ChartTrendlineData>> AddDataSetsToChart()
    {
        var dataSetIndex = 0;

        Colours = new ColourServer();

        var trendlines = new List<ChartTrendlineData>();

        var requestedColours = ChartSeriesWithData!
            .Where(x => x.ChartSeries!.RequestedColour != UiLogic.Colours.AutoAssigned)
            .Select(x => x.ChartSeries!.RequestedColour)
            .ToList();

        foreach (var chartSeries in ChartSeriesWithData!)
        {
            var dataSet = chartSeries.ProcessedDataSet!;
            var htmlColourCode = Colours.GetNextColour(chartSeries.ChartSeries!.RequestedColour, (List<Colours>)requestedColours);
            var renderSmallPoints = (bool)IsMobileDevice! || dataSet.DataRecords.Count() > 400;
            var defaultLabel = (bool)IsMobileDevice!
                ? chartSeries.ChartSeries.GetFriendlyTitleShort()
                : $"{chartSeries.ChartSeries.FriendlyTitle} | {UnitOfMeasureLabelShort(dataSet.MeasurementDefinition!.UnitOfMeasure)}";

            await ChartLogic.AddDataSetToChart(
                chart!,
                chartSeries,
                dataSet,
                GetChartLabel(chartSeries.ChartSeries.SeriesTransformation, defaultLabel, chartSeries.ChartSeries.Aggregation),
                htmlColourCode,
                renderSmallPoints: renderSmallPoints);

            if (chartSeries.ChartSeries.ShowTrendline)
            {
                trendlines.Add(ChartLogic.CreateTrendline(dataSetIndex, ChartColor.FromHtmlColorCode(htmlColourCode)));
            }

            dataSetIndex++;
        }

        return trendlines;
    }

    private void RebuildChartSeriesListToReflectSelectedYears()
    {
        var years = SelectedYears!.Any() ? SelectedYears!.Select(x => (short?)x).ToList() : new List<short?>() { null };

        List<ChartSeriesDefinition> newCsds = new List<ChartSeriesDefinition>();

        var uniqueChartSeriesList = ChartSeriesList!.Distinct(new ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked()).ToArray();

        foreach (var csd in uniqueChartSeriesList)
        {
            foreach (var year in years)
            {
                newCsds.Add(
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = csd.SourceSeriesSpecifications,
                        Aggregation = csd.Aggregation,
                        BinGranularity = year == null ? BinGranularities.ByYear : BinGranularities.ByYearAndMonth,
                        DisplayStyle = csd.DisplayStyle,
                        IsLocked = csd.IsLocked,
                        ShowTrendline = csd.ShowTrendline,
                        SecondaryCalculation = csd.SecondaryCalculation,
                        Smoothing = csd.Smoothing,
                        SmoothingWindow = csd.SmoothingWindow,
                        Value = csd.Value,
                        Year = year,
                        SeriesTransformation = csd.SeriesTransformation,
                        GroupingThreshold = csd.GroupingThreshold,
                        MinimumDataResolution = csd.MinimumDataResolution,
                    });
            }
        }

        Logger!.LogInformation("RebuildChartSeriesListToReflectSelectedYears() setting ChartSeriesList");
        ChartSeriesList = newCsds;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    private void BuildProcessedDataSets(List<SeriesWithData> chartSeriesWithData, bool chartAllData)
    {
        var l = new LogAugmenter(Logger!, "BuildProcessedDataSets");

        l.LogInformation("entering");

        foreach (var cs in chartSeriesWithData)
        {
            if (cs.ChartSeries!.SecondaryCalculation == SecondaryCalculationOptions.AnnualChange)
            {
                var yearlyDifferenceValues =
                    cs.SourceDataSet!.DataRecords
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
                    };
            }
        }

        var badChartSeries = new List<SeriesWithData>();

        // If we're doing smoothing via the moving average, precalculate these data and add them to PreProcessedDataSets.
        // We do this because the SimpleMovingAverage calculate function will remove some years from the start of the data set.
        // It removes these years because it doesn't have a good enough average to present it to the user.
        // Therefore, we need to calculate the smoothing before we calculate the start year - the basis for labelling the chart
        // If we're not calculating a moving average, PreProcessedDataSets = SourceDataSets
        foreach (var cs in chartSeriesWithData)
        {
            // We only support moving averages on linear bin granularities (e.g. Year, YearAndMonth) - not modular ones like MonthOnly
            if (SelectedBinGranularity.IsLinear() && cs.ChartSeries!.Smoothing == SeriesSmoothingOptions.MovingAverage)
            {
                var movingAverageValues =
                    cs.SourceDataSet!.DataRecords
                    .Select(x => x.Value)
                    .CalculateCentredMovingAverage(cs.ChartSeries.SmoothingWindow, 0.75f);

                if (movingAverageValues.All(y => y == null))
                {
                    l.LogError("Moving average calculation has resulted in no records. Will remove this series from the chart.");
                    badChartSeries.Add(cs);
                    continue;
                }

                // Now, join back to the original DataRecord set
                var newDataRecords =
                    movingAverageValues
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
                    };
            }
            else
            {
                cs.PreProcessedDataSet = cs.SourceDataSet;
            }
        }

        badChartSeries.ForEach(x => chartSeriesWithData.Remove(x));

        l.LogInformation("done with moving average calculation");

        // There must be exactly one bin granularity or else something odd's going on.
        var binGranularity = chartSeriesWithData.Select(x => x.ChartSeries!.BinGranularity).Distinct().Single();

        if (binGranularity != SelectedBinGranularity)
        {
            throw new Exception($"BinGranularity selected for series ({binGranularity}) doesn't match overall selected granularity {SelectedBinGranularity}");
        }

        BinIdentifier[]? chartBins = null;

        switch (binGranularity)
        {
            case BinGranularities.ByYear:
            case BinGranularities.ByYearAndMonth:
            case BinGranularities.ByYearAndWeek:
            case BinGranularities.ByYearAndDay:
                // Calculate first and last year which we have a data record for, across all data sets underpinning all chart series
                var preProcessedDataSets = chartSeriesWithData.Select(x => x.PreProcessedDataSet);
                var allDataRecords = preProcessedDataSets.SelectMany(x => x!.DataRecords);

                (chartStartBin, chartEndBin) =
                    ChartLogic.GetBinRangeToPlotForGaplessRange(
                        preProcessedDataSets!, // Pass in the data available for plotting
                        chartAllData, // and the user's preferences about what x axis range they'd like plotted
                        SelectedStartYear!,
                        SelectedEndYear!);

                chartBins = BinHelpers.EnumerateBinsInRange(chartStartBin, chartEndBin).ToArray();

                SetStartAndEndYears(chartSeriesWithData);

                break;

            case BinGranularities.ByMonthOnly:
            case BinGranularities.BySouthernHemisphereTemperateSeasonOnly:
            case BinGranularities.BySouthernHemisphereTropicalSeasonOnly:
                chartStartBin = null;
                chartEndBin = null;
                chartBins = BinHelpers.GetBinsForModularGranularity(binGranularity);
                break;

            default:
                throw new NotImplementedException($"binGranularity {binGranularity}");
        }

        foreach (var cs in chartSeriesWithData)
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
                    DataRecords =
                        chartBins
                        .Select(
                            bin =>

                            // If there's a record in the source dataset, use it
                            recordsByBinId[bin.Id].SingleOrDefault()

                            // Otherwise, create a null record
                            ?? new BinnedRecord(bin.Id, null))
                        .ToList(),
                };
        }

        ChartBins = chartBins;

        // Now, we cut down the processed datasets to just the bins that we intend to display on the chart.
        // This should only affect linear (gapless) BinGranularities, but executes either way, in case we
        // later allow users to say "just give me month-ignoring-year, but only for months after 4 and before 7",
        // for example.
        var binIdsToPlot = new HashSet<string>(ChartBins.Select(x => x.Id));
        foreach (var cswd in ChartSeriesWithData!)
        {
            cswd.ProcessedDataSet!.DataRecords =
                cswd.ProcessedDataSet.DataRecords
                .Where(x => binIdsToPlot.Contains(x.BinId!))
                .ToList();
        }

        l.LogInformation("leaving");
    }

    private string GetChartLabel(SeriesTransformations seriesTransformation, string defaultLabel, SeriesAggregationOptions seriesAggregationOptions)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.IsFrosty => "Number of days of frost",
            SeriesTransformations.DayOfYearIfFrost => seriesAggregationOptions == SeriesAggregationOptions.Maximum ? "Last day of frost" : "First day of frost",
            SeriesTransformations.EqualOrAbove25 => "Number of days 25°C or above",
            SeriesTransformations.EqualOrAbove35 => "Number of days 35°C or above",
            SeriesTransformations.EqualOrAbove1 => "Number of days with 1mm of rain or more",
            SeriesTransformations.EqualOrAbove1AndLessThan10 => "Number of days between 1mm and 10mm of rain",
            SeriesTransformations.EqualOrAbove10 => "Number of days with 10mm of rain or more",
            SeriesTransformations.EqualOrAbove10AndLessThan25 => "Number of days between 10mm and 25mm of rain",
            SeriesTransformations.EqualOrAbove25mm => "Number of days with 25mm of rain or more",
            _ => defaultLabel,
        };
    }

    private dynamic BuildChartScales()
    {
        dynamic scales = new ExpandoObject();

        var xLabel = ChartLogic.GetXAxisLabel(SelectedBinGranularity);

        // The casing on the names of the scales is case-sensitive.
        // The x axis needs to be x (not X) or the chart will not register the scale correctly.
        // This will at least break the trendlines component
        scales.x = new
        {
            Grid = new { DrawOnChartArea = false },
            Title = new
            {
                Text = xLabel,
                Display = true,
                Color = "black",
            },
        };

        var axes = new List<string>();
        foreach (var s in ChartSeriesList!.Where(x => x.DataAvailable))
        {
            var uom = s.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure;
            var axisId = ChartLogic.GetYAxisId(s.SeriesTransformation, uom, s.Aggregation);
            if (!axes.Contains(axisId))
            {
                ((IDictionary<string, object>)scales).Add(
                    axisId,
                    new
                    {
                        Display = true,
                        Axis = "y",
                        Position = axes.Count % 2 == 0 ? "left" : "right",
                        Grid = new { DrawOnChartArea = axes.Count == 0 },
                        Title = new
                        {
                            Text = UnitOfMeasureLabel(s.SeriesTransformation, uom, s.Aggregation, s.Value),
                            Display = true,
                            Color = s.Colour,
                        },
                    });
                axes.Add(axisId);
            }
        }

        return scales;
    }

    private async Task OnSelectedBinGranularityChanged(BinGranularities value, bool rebuildDataSets = true)
    {
        SelectedBinGranularity = value;

        foreach (var csd in ChartSeriesList!)
        {
            csd.BinGranularity = value;
        }

        ChartSeriesList = ChartSeriesList.CreateNewListWithoutDuplicates();

        if (SelectedBinGranularity == BinGranularities.ByYearAndMonth
            || SelectedBinGranularity == BinGranularities.ByYearAndWeek
            || SelectedBinGranularity == BinGranularities.ByYearAndDay)
        {
            await ShowRangeSliderChanged(true);
        }

        if (rebuildDataSets)
        {
            await BuildDataSets();
        }
    }

    private void OnGroupingThresholdTextChanged(string value)
    {
        GroupingThresholdText = value;
    }

    private async Task ApplyYearlyAverageParameters()
    {
        UserOverridePresetAggregationSettings = true;
        InternalGroupingThreshold = float.Parse(GroupingThresholdText!) / 100;
        SelectedGroupingDays = SelectingGroupingDays == 0 ? SelectedGroupingDays : SelectingGroupingDays;
        await BuildDataSets();
    }

    private string GetGroupingThresholdText()
    {
        var groupingThreshold = ChartSeriesList!.FirstOrDefault() == null ? null : ChartSeriesList!.First().GroupingThreshold;

        return UserOverridePresetAggregationSettings
            ? $"{MathF.Round(InternalGroupingThreshold * 100, 0)}% (user override)"
            : groupingThreshold == null
                    ? $"{MathF.Round(InternalGroupingThreshold * 100, 0)}%"
                    : $"{MathF.Round((float)groupingThreshold * 100, 0)}% (preset defined)";
    }

    private async Task OnLineChartClicked(ChartMouseEventArgs e)
    {
        if (SelectedBinGranularity != BinGranularities.ByYear)
        {
            // TODO: Add support for SelectedBinGranularity != BinGranularities.ByYear
            return;
        }

        int startYear = ChartAllData ? StartYears!.First() : StartYears!.Last();

        var year = (short)(startYear + e.Index);

        var dataType = ChartSeriesWithData![e.DatasetIndex].SourceDataSet!.DataType;
        var dataAdjustment = ChartSeriesWithData[e.DatasetIndex].SourceDataSet!.DataAdjustment;

        await HandleOnYearFilterChange(new YearAndDataTypeFilter(year) { DataType = dataType, DataAdjustment = dataAdjustment });
    }

    private async Task ShowRangeSliderChanged(bool? value)
    {
        EnableRangeSlider = value;
        if (EnableRangeSlider.GetValueOrDefault() && SliderStart == null)
        {
            SetStartAndEndYears(ChartSeriesWithData!);

            var proportionToShow = SelectedBinGranularity == BinGranularities.ByYearAndDay ? 0.05f
                                 : SelectedBinGranularity == BinGranularities.ByYearAndWeek ? 0.1f
                                 : SelectedBinGranularity == BinGranularities.ByYearAndMonth ? 0.15f
                                 : .3f;
            var rangeStart = (int)MathF.Ceiling((EndYear - StartYears!.Max()) * proportionToShow);
            await OnStartYearTextChanged((EndYear - rangeStart).ToString());
        }
    }

    private async Task OnSelectedYearsChanged(ExtentValues extentValues)
    {
        await ChangeStartYear(extentValues.FromValue!, false);
        await ChangeEndYear(extentValues.ToValue!, false);
        await HandleRedraw();
    }

    private async Task OnStartYearTextChanged(string? text)
    {
        await ChangeStartYear(text, true);
    }

    private async Task ChangeStartYear(string? text, bool redraw)
    {
        SelectedStartYear = text;
        if (SelectedStartYear != null)
        {
            SliderStart = Convert.ToInt32(SelectedStartYear);
            if (redraw)
            {
                await HandleRedraw();
            }
        }
    }

    private async Task OnEndYearTextChanged(string text)
    {
        await ChangeEndYear(text, true);
    }

    private async Task ChangeEndYear(string text, bool redraw)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            SelectedEndYear = null;
            SliderEnd = null;
        }
        else
        {
            SelectedEndYear = text;
            SliderEnd = Convert.ToInt32(SelectedEndYear);
        }

        if (redraw)
        {
            await HandleRedraw();
        }
    }

    private async Task OnDynamicStartYearChanged(bool? value)
    {
        ChartAllData = value.GetValueOrDefault();
        if (value != null)
        {
            SliderStart = null;
            SelectedStartYear = null;
            await HandleRedraw();
        }
    }

    private void OnSelectedGroupingDaysChanged(short value)
    {
        SelectingGroupingDays = value;
    }

    private async Task ClearUserAggregationOverride()
    {
        UserOverridePresetAggregationSettings = false;
        await BuildDataSets();
    }

    private void SetStartAndEndYears(List<SeriesWithData> chartSeriesWithData)
    {
        var binGranularity = chartSeriesWithData.Select(x => x.ChartSeries!.BinGranularity).Distinct().Single();
        var dataSet = binGranularity == BinGranularities.ByYear ? chartSeriesWithData.Select(x => x.PreProcessedDataSet) : chartSeriesWithData.Select(x => x.SourceDataSet);

        // build a list of all the years in which data sets start, used by the UI to allow the user to conveniently select from them
        StartYears = dataSet.Select(x => x!.GetStartYearForDataSet()).Distinct().OrderBy(x => x).ToList();
        SliderMin = StartYears.Min();
        if (SliderStart < SliderMin)
        {
            SliderStart = SliderMin;
        }

        var lastYears = dataSet.Select(x => x!.GetEndYearForDataSet()).Distinct().OrderBy(x => x).ToList();
        SliderMax = EndYear = lastYears.Max();
        if (SliderEnd > SliderMax)
        {
            SliderEnd = SliderMax;
        }
    }

    private async Task OnClearFilter()
    {
        SelectedStartYear = null;
        SelectedEndYear = null;
        SliderStart = null;
        SliderEnd = null;
        EnableRangeSlider = false;

        await BuildDataSets();
    }

    private async Task BuildDataSets()
    {
        await BuildDataSetsEvent.InvokeAsync();
    }

    private async Task OnDownloadDataClicked()
    {
        await DownloadDataEvent.InvokeAsync(new DataDownloadPackage { ChartSeriesWithData = ChartSeriesWithData!, Bins = ChartBins!, BinGranularity = SelectedBinGranularity });
    }

    private async Task ShowAddDataSetModal()
    {
        await ShowAddDataSetModalEvent.InvokeAsync();
    }

    private Task ShowOptionsModal()
    {
        GroupingThresholdText = MathF.Round(InternalGroupingThreshold * 100, 0).ToString();
        return OptionsModal!.Show();
    }

    private string GroupingDaysText(int groupingDays)
    {
        return groupingDays switch
        {
            5 => "Groups of 5 days (73 groups)",
            7 => "Groups of 7 days (52 groups)",
            13 => "Groups of 13 days (28 groups)",
            14 => "Groups of 14 days (26 groups)",
            26 => "Groups of 26 days (14 groups)",
            28 => "Groups of 28 days (13 groups)",
            73 => "Groups of 73 days (5 groups)",
            91 => "Groups of 91 days (4 groups)",
            182 => "Groups of 182 days (2 groups)",
            _ => throw new NotImplementedException(groupingDays.ToString()),
        };
    }
}
