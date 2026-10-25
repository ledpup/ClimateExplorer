namespace ClimateExplorer.Web.Client.Components.Chart;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using CurrentDevice;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class ChartView : IDisposable
{
    private bool haveCalledResizeAtLeastOnce = false;
    private bool disposed;

    private ChartJsChart? chart;
    private ElementReference chartWrapper;

    private BinIdentifier? chartStartBin;
    private BinIdentifier? chartEndBin;

    private InfoPanel? chartOptionsInfoPanel;

    private string? groupingThresholdText;

    private bool hasRenderableChartData;
    private ChartState? appliedState;
    private ChartDataBuildResult? renderedData;
    private bool renderedLoadingErrored;
    private IUnitFormatter? renderedUnits;
    private bool renderChartInProcess;

    [Parameter]
    public ChartState? State { get; set; }

    [Parameter]
    public ChartDataBuildResult? Data { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public bool LoadingErrored { get; set; }

    [Parameter]
    public EventCallback<ChartState> ChartStateChanged { get; set; }

    [Parameter]
    public EventCallback<DataDownloadPackage> DownloadRequested { get; set; }

    [Parameter]
    public EventCallback AddDataSetRequested { get; set; }

    [Parameter]
    public EventCallback<YearAndDataTypeFilter> YearFilterRequested { get; set; }

    /// <summary>
    /// Full location details for the chart title. A data set's GeographicalEntity can't be used: it
    /// deserialises from the API as the base type, without the country.
    /// </summary>
    [Parameter]
    public Dictionary<Guid, Location>? Locations { get; set; }

    /// <summary>
    /// Only here so that choosing another unit system re-renders this component, which then redraws the
    /// chart. Drawing uses <see cref="UnitPreference"/>, whose stored choice the first draw waits for.
    /// </summary>
    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    [Inject]
    private UnitPreference? UnitPreference { get; set; }

    [Inject]
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    [Inject]
    private ILogger<ChartView>? Logger { get; set; }

    private bool ChartLoadingIndicatorVisible { get; set; }
    private bool ChartLoadingErrored { get; set; }
    private bool NoChartDataAvailable { get; set; }

    // Internal view state: whether the chart plots all available years or the filtered range. Surfaced
    // and updated via ChartState (CreateCurrentChartState/SetChartState) rather than as a parameter.
    private bool ChartAllData { get; set; }

    private BinGranularities SelectedBinGranularity { get; set; } = BinGranularities.ByYear;
    private List<SeriesWithData>? ChartSeriesWithData { get; set; }
    private List<SeriesWithData>? AllChartSeriesWithData { get; set; }
    private string? SelectedStartYear { get; set; }
    private string? SelectedEndYear { get; set; }
    private short SelectedGroupingDays { get; set; }
    private string? GroupingThresholdText
    {
        get => groupingThresholdText;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                groupingThresholdText = value;
                InternalGroupingThreshold = float.Parse(value) / 100;
            }
        }
    }

    private List<ChartSeriesDefinition>? ChartSeriesList { get; set; } = [];

    private float InternalGroupingThreshold { get; set; } = .7f;

    private bool UserOverridePresetAggregationSettings { get; set; }

    private BinIdentifier[]? ChartBins { get; set; }

    private bool? IsMobileDevice { get; set; }

    private List<short>? StartYears { get; set; }

    private List<AxisInfo> CurrentAxes { get; set; } = [];

    private Dictionary<string, bool> AxesScaleToZero { get; set; } = [];

    // The ChartJsChart child destroys the Chart.js instance when it is disposed.
    public void Dispose()
    {
        disposed = true;
    }

    protected override void OnInitialized()
    {
        ChartLoadingIndicatorVisible = true;
        ChartLoadingErrored = false;

        SelectedGroupingDays = 14;
        GroupingThresholdText = "70";
    }

    protected override void OnParametersSet()
    {
        ChartLoadingIndicatorVisible = IsLoading;
        ChartLoadingErrored = LoadingErrored;

        if (IsLoading)
        {
            NoChartDataAvailable = false;
        }

        if (State is not null && !ReferenceEquals(State, appliedState))
        {
            SetChartState(State);
            appliedState = State;
        }

        if (Data is null && !IsLoading)
        {
            NoChartDataAvailable = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        IsMobileDevice ??= await CurrentDeviceService!.Mobile();

        if (IsLoading)
        {
            return;
        }

        // A change of unit system redraws from the same data: nothing is refetched or recalculated.
        if (Data is not null && !renderChartInProcess && (!ReferenceEquals(Data, renderedData) || LoadingErrored != renderedLoadingErrored || !ReferenceEquals(UnitPreference!.Formatter, renderedUnits)))
        {
            renderedData = Data;
            renderedLoadingErrored = LoadingErrored;
            renderChartInProcess = true;

            try
            {
                // Wait for the stored unit choice, so a US customary reader's first chart is never drawn in metric.
                await UnitPreference!.LoadAsync();
                renderedUnits = UnitPreference.Formatter;

                ApplyChartData(Data);
                await RenderChart(renderedUnits);
            }
            catch
            {
                renderedData = null;
                renderedUnits = null;
                throw;
            }
            finally
            {
                renderChartInProcess = false;
            }
        }
    }

    protected async Task RenderChart(IUnitFormatter units)
    {
        var l = new LogAugmenter(Logger!, "RenderChart");

        l.LogInformation("Entering");

        if (disposed)
        {
            l.LogInformation("Bailing early as component is disposed");
            ChartLoadingIndicatorVisible = false;
            return;
        }

        if (chart is null)
        {
            l.LogInformation("Bailing early as chart is unavailable");
            NoChartDataAvailable = true;
            ChartLoadingIndicatorVisible = false;
            StateHasChanged();
            return;
        }

        try
        {
            await chart.ClearAsync();
        }
        catch (Exception ex)
        {
            Logger!.LogError(ex, "Failed to clear the chart before rendering.");
            NoChartDataAvailable = true;
            ChartLoadingIndicatorVisible = false;
            StateHasChanged();
            return;
        }

        var title = string.Empty;
        var subtitle = string.Empty;
        List<ChartTooltipSeriesInfo> tooltipMetadata = [];

        if (ChartLoadingErrored)
        {
            await chart.SetOptionsAsync(new { });
            NoChartDataAvailable = true;

            l.LogError("We have identified an error. Will not try to render the chart normally");
        }
        else if (ChartSeriesWithData == null || ChartSeriesWithData.Count == 0)
        {
            await chart.SetOptionsAsync(new { });

            NoChartDataAvailable = true;
            ChartLoadingIndicatorVisible = false;
            StateHasChanged();

            l.LogInformation("Bailing early as no chart data available");
            return;
        }
        else
        {
            LogChartSeriesList();

            // If any series is in "Bar" mode, we switch the entire chart to Bar type to ensure it
            // renders, at the cost of a small misalignment between grid lines and datapoints for any
            // line series that are being displayed. Otherwise, we display in "Line" mode to avoid that
            // cost. "Scatter" series are also rendered using "Line" mode, as a line dataset with no
            // connecting line and larger points.
            var chartType =
                ChartSeriesWithData.Any(x => x.ChartSeries!.DisplayStyle == SeriesDisplayStyle.Bar)
                ? ChartJsType.Bar
                : ChartJsType.Line;

            // The data has already been fetched and prepared by the parent (gap filling, smoothing,
            // secondary calculations, bin selection). RenderChart consumes that prepared state directly.
            if (!hasRenderableChartData)
            {
                await chart.SetOptionsAsync(new { });
                NoChartDataAvailable = true;
                ChartLoadingIndicatorVisible = false;
                StateHasChanged();

                l.LogWarning("Bailing early because processed chart datasets contain no renderable values.");
                return;
            }

            NoChartDataAvailable = false;

            title = ChartLogic.BuildChartTitle(ChartSeriesWithData, Locations);
            subtitle = ChartLogic.BuildChartSubtitle(chartStartBin, chartEndBin, SelectedBinGranularity);
            var aggregationText = ChartLogic.BuildAggregationText(SelectedGroupingDays, GetGroupingThresholdText());

            l.LogInformation("Building datasets");

            (var datasets, tooltipMetadata) = ChartDisplayBuilder.BuildDatasets(ChartSeriesWithData, ChartBins!, IsMobileDevice!.Value, units);

            var labels = ChartBins!.Select(x => x.Label).ToArray();
            await chart.SetDataAsync(chartType, labels, datasets);

            var optionsResult = ChartOptionsFactory.Build(new ChartOptionsRequest
            {
                Title = title,
                Subtitle = subtitle,
                AggregationText = aggregationText,
                BinGranularity = SelectedBinGranularity,
                IsMobileDevice = IsMobileDevice!.Value,
                SeriesWithData = ChartSeriesWithData,
                Series = ChartSeriesList!,
                AxesScaleToZero = AxesScaleToZero,
                Units = units,
            });

            CurrentAxes = [.. optionsResult.Axes];

            await chart.SetOptionsAsync(optionsResult.Options);
        }

        // These customise the options and datasets set above, so they have to be in place before the
        // update. Applying them afterwards left the tooltip intermittently unhooked.
        await JsRuntime!.InvokeVoidAsync("configureChartTooltip", chartWrapper, tooltipMetadata);
        await JsRuntime!.InvokeVoidAsync("configureShrunkWindowSegments", chartWrapper, tooltipMetadata);

        await chart.UpdateAsync();

        await JsRuntime!.InvokeVoidAsync("registerChartHoverCursor", chartWrapper);

        // The below line is required to get the chart.js component to honour the styling applied on the parent div
        // If you don't call resize, the chart will apply the styling only after you resize the window,
        // but it does not apply the style on the initial load of the page.
        // See https://www.chartjs.org/docs/latest/configuration/responsive.html for more information
        if (!haveCalledResizeAtLeastOnce && !ChartLoadingErrored)
        {
            await chart.ResizeAsync();
            haveCalledResizeAtLeastOnce = true;
        }

        ChartLoadingIndicatorVisible = false;
        StateHasChanged();

        l.LogInformation("Leaving");
    }

    private ChartState CreateCurrentChartState()
    {
        return new ChartState
        {
            ChartAllData = ChartAllData,
            StartYear = SelectedStartYear,
            EndYear = SelectedEndYear,
            GroupingDays = SelectedGroupingDays,
            GroupingThresholdText = GroupingThresholdText ?? string.Empty,
            UserOverrideAggregationSettings = UserOverridePresetAggregationSettings,
            AxesScaleToZero = AxesScaleToZero,
            Series = ChartSeriesList ?? [],
        };
    }

    private void SetChartState(ChartState state)
    {
        ChartAllData = state.ChartAllData;
        SelectedStartYear = state.StartYear;
        SelectedEndYear = state.EndYear;
        SelectedGroupingDays = state.GroupingDays;
        GroupingThresholdText = state.GroupingThresholdText;
        UserOverridePresetAggregationSettings = state.UserOverrideAggregationSettings;
        AxesScaleToZero = state.AxesScaleToZero;

        if (state.Series.Any())
        {
            SelectedBinGranularity = state.Series.First().BinGranularity;
        }

        Logger!.LogInformation("Setting ChartSeriesList to list with " + state.Series.Count + " items");

        ChartSeriesList = state.Series.ToList();
    }

    private void ApplyChartData(ChartDataBuildResult buildResult)
    {
        ChartSeriesWithData = [.. buildResult.SeriesWithData];
        AllChartSeriesWithData = [.. buildResult.SeriesWithData, .. buildResult.NonRenderedSeriesWithData];
        ChartBins = buildResult.ChartBins;
        chartStartBin = buildResult.ChartStartBin;
        chartEndBin = buildResult.ChartEndBin;
        StartYears = [.. buildResult.StartYears];
        hasRenderableChartData = buildResult.HasRenderableData;

        Logger!.LogInformation("Applied chart data. ChartSeriesWithData now has " + ChartSeriesWithData.Count + " entries.");
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
        await RaiseChartStateChanged();
    }

    private Task ShowChartOptionsInfo()
    {
        return chartOptionsInfoPanel!.ShowAsync();
    }

    /// <summary>
    /// The last year the chart holds real data for. This is <c>chartEndBin</c> rather than the last
    /// bin, because the bin array is deliberately extended past the data to make room for trend
    /// projections.
    /// </summary>
    private short LastYearWithData()
    {
        return chartEndBin is YearBinIdentifier yearBin ? yearBin.Year : short.MaxValue;
    }

    private async Task OnSelectedBinGranularityChanged(BinGranularities value, bool rebuildDataSets = true)
    {
        SelectedBinGranularity = value;

        foreach (var csd in ChartSeriesList!)
        {
            csd.BinGranularity = value;
        }

        ChartSeriesList = ChartSeriesList.CreateNewListWithoutDuplicates();

        if (rebuildDataSets)
        {
            await RaiseChartStateChanged();
        }
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

    private async Task OnLineChartClicked(ChartJsClickEventArgs e)
    {
        if (SelectedBinGranularity != BinGranularities.ByYear)
        {
            // TODO: Add support for SelectedBinGranularity != BinGranularities.ByYear
            return;
        }

        if (chartStartBin is not YearBinIdentifier startBin)
        {
            return;
        }

        int startYear = startBin.Year;

        var year = (short)(startYear + e.Index);

        // Clicks can land on a projected year (the bins run past the end of the data whenever a
        // trend is displayed) or on a trend dataset itself. Neither has source data to filter to.
        if (e.DatasetIndex >= ChartSeriesWithData!.Count || year > LastYearWithData())
        {
            return;
        }

        var sourceDataSet = ChartSeriesWithData![e.DatasetIndex].SourceDataSet!;

        await YearFilterRequested.InvokeAsync(new YearAndDataTypeFilter(year) { DataType = sourceDataSet.DataType, DataAdjustment = sourceDataSet.DataAdjustment, UnitOfMeasure = sourceDataSet.MeasurementDefinition!.UnitOfMeasure });
    }

    private async Task OnClearFilter()
    {
        SelectedStartYear = null;
        SelectedEndYear = null;

        await RaiseChartStateChanged();
    }

    private async Task OnDownloadDataClicked()
    {
        // Bins past the end of the data exist only to make room for trend projections; exporting
        // them would append rows of empty values to the CSV.
        var binsWithData = ChartBins!
            .Where(x => x is not YearBinIdentifier yearBin || yearBin.Year <= LastYearWithData())
            .ToArray();

        await DownloadRequested.InvokeAsync(new DataDownloadPackage { ChartSeriesWithData = ChartSeriesWithData!, Bins = binsWithData, BinGranularity = SelectedBinGranularity, Units = renderedUnits ?? UnitFormatter.Metric });
    }

    private async Task ShowAddDataSetModal()
    {
        await AddDataSetRequested.InvokeAsync();
    }

    private async Task OnAggregationSettingsChanged(AggregationSettings settings)
    {
        GroupingThresholdText = settings.ThresholdText;
        SelectedGroupingDays = settings.GroupingDays;
        UserOverridePresetAggregationSettings = settings.UserOverride;
        await RaiseChartStateChanged();
    }

    private async Task RaiseChartStateChanged()
    {
        await ChartStateChanged.InvokeAsync(CreateCurrentChartState());
    }
}
