namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Components.Info;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class LocationDashboard
{
    private SidePanel? climateRecordsSidePanel;
    private SidePanel? recentObservationsSidePanel;
    private LocationDataSetMetadataSidePanel? dataSetMetadataSidePanel;

    [Inject]
    public UiLogic.DateLabels DateLabels { get; set; } = default!;

    [Inject]
    public IDataService? DataService { get; set; }

    [Inject]
    public ILogger<LocationDashboard>? Logger { get; set; }

    [Inject]
    public IJSRuntime? JS { get; set; }

    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    public EventCallback RequestLocationChange { get; set; }

    [Parameter]
    public bool ChangeLocationLoading { get; set; }

    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public EventCallback<bool> OnOverviewShowOrHide { get; set; }

    [Parameter]
    public EventCallback<YearAndDataTypeFilter> OnYearFilterChange { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private Collapsible? LocationCollapsible { get; set;  }

    private string LocationMapUrl => Location == null ? "https://maps.google.com/" : $"https://maps.google.com/?q={Location.Coordinates.Latitude},{Location.Coordinates.Longitude}";
    private string? GeoLocationAsString { get; set; }

    private bool LocationLoadingIndicatorVisible { get; set; }
    private bool StripeLoadingIndicatorVisible { get; set; }
    private bool LoadStripeData { get; set; }

    private bool DisplayPrecipitation { get; set; }

    private List<YearlyValues>? TemperatureAnomalyRecords { get; set; }
    private double? TemperatureLocationMean { get; set; }
    private RenderFragment? WarmingAnomalyContent { get; set; }

    // The location the overview fields describe, set once per location. The fields themselves are
    // formatted at render, so they follow a change of unit system without recalculating anything.
    private Location? OverviewLocation { get; set; }

    private string? WarmingAnomalyAsString => OverviewLocation is null
        ? null
        : ApiMeasures.WarmingAnomaly(OverviewLocation) is { } anomaly ? Units.Format(anomaly, signed: true) : "NA";

    private LocationTrendField? TemperatureTrendField => OverviewLocation is null ? null : LocationTrendFieldBuilder.Build(OverviewLocation, Units);
    private RenderFragment? TemperatureTrendContent { get; set; }

    private List<YearlyValues>? PrecipitationAnomalyRecords { get; set; }
    private double? PrecipitationLocationMean { get; set; }
    private RenderFragment? PrecipitationAnomalyContent { get; set; }
    private bool PrecipitationAnomalyLoaded { get; set; }
    private CalculatedAnomaly? PrecipitationAnomaly { get; set; }

    private string? PrecipitationAnomalyAsString => !PrecipitationAnomalyLoaded
        ? null
        : PrecipitationAnomaly is null ? "NA" : Units.Format(ApiMeasures.Anomaly(PrecipitationAnomaly, UnitOfMeasure.Millimetres), signed: true);

    private Guid? LocationIdLastTimeOnParametersSetAsyncWasCalled { get; set; }

    private DataType? TemperatureDataType { get; set; }

    private RenderFragment? HeatingScoreContent { get; set; }

    private string? RecordHighToolTip
    {
        get
        {
            if (OverviewLocation?.RecordHigh is not { } recordHigh || ApiMeasures.RecordHigh(OverviewLocation) is not { } value)
            {
                return null;
            }

            var recordHighDate = recordHigh.Day == null
                ? $"{CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(recordHigh.Month!.Value)} {recordHigh.Year}"
                : DateLabels.ShortDayMonthYear(new DateOnly(recordHigh.Year, recordHigh.Month!.Value, recordHigh.Day.Value));

            return $"{OverviewLocation.Name} record high of {Units.Format(value)} set {recordHighDate}.<br>Click for more records.";
        }
    }

    private string DataSourcesButtonLabel => Location is null
        ? "View data sources"
        : $"View data sources for {Location.Name}";

    public void ChangeLocationClicked(EventArgs args)
    {
        RequestLocationChange.InvokeAsync();
    }

    public void OverviewShowOrHideHandler(bool showOrHide)
    {
        OnOverviewShowOrHide.InvokeAsync(showOrHide);
    }

    public Task ShowRecordHighAsync() => climateRecordsSidePanel?.ShowAsync() ?? Task.CompletedTask;

    public Task ShowRecentObservationsAsync() => recentObservationsSidePanel?.ShowAsync() ?? Task.CompletedTask;

    public Task ShowDataSetMetadataAsync()
    {
        return Location is null
            ? Task.CompletedTask
            : dataSetMetadataSidePanel?.ShowAsync(Location) ?? Task.CompletedTask;
    }

    public async Task OpenLocationMapAsync() => await JS!.InvokeVoidAsync("open", LocationMapUrl, "_blank");

    protected override void OnInitialized()
    {
        LocationLoadingIndicatorVisible = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Location == null || DataSetDefinitions == null)
        {
            return;
        }

        // We do manual change detection here because Blazor calls OnParametersSetAsync() repeatedly with the same input values.
        if (LocationIdLastTimeOnParametersSetAsyncWasCalled == Location?.Id)
        {
            return;
        }

        GeoLocationAsString = Location!.Coordinates.ToString();
        LocationIdLastTimeOnParametersSetAsyncWasCalled = Location?.Id;
        LocationLoadingIndicatorVisible = false;
        LoadStripeData = true;
        StripeLoadingIndicatorVisible = true;

        OverviewLocation = Location;

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Location == null || DataSetDefinitions == null)
        {
            return;
        }

        if (LoadStripeData)
        {
            LoadStripeData = false;

            Logger!.LogInformation("Loading data");

            var temperatureAnomaly = await CalculateAnomaly(DataSubstitute.StandardTemperatureDataMatches(), ContainerAggregationFunctions.Mean);
            if (temperatureAnomaly != null)
            {
                TemperatureDataType = temperatureAnomaly.DataSet?.DataType;
                WarmingAnomalyContent = Location == null
                        ? null
                        : builder =>
                        {
                            builder.OpenComponent(0, typeof(WarmingAnomalyInfo));
                            builder.AddAttribute(1, "Location", Location);
                            builder.AddAttribute(2, "CalculatedAnomaly", temperatureAnomaly.CalculatedAnomaly);
                            builder.CloseComponent();
                        };
                TemperatureTrendContent = Location == null
                        ? null
                        : builder =>
                        {
                            builder.OpenComponent(0, typeof(TemperatureTrendInfo));
                            builder.AddAttribute(1, "Location", Location);
                            builder.AddAttribute(2, "DataSet", temperatureAnomaly.DataSet);
                            builder.CloseComponent();
                        };
                TemperatureLocationMean = temperatureAnomaly?.DataSet?.DataRecords.Average(x => x.Value);
                TemperatureAnomalyRecords = temperatureAnomaly?.AnomalyRecords;
            }
            else
            {
                TemperatureDataType = null;
                WarmingAnomalyContent = null;
                TemperatureTrendContent = null;
                TemperatureLocationMean = null;
                TemperatureAnomalyRecords = null;
            }

            HeatingScoreContent = Location == null
                                ? null
                                : builder =>
                                {
                                    builder.OpenComponent(0, typeof(HeatingScoreInfo));
                                    builder.AddAttribute(1, "Location", Location);
                                    builder.CloseComponent();
                                };

            if (DisplayPrecipitation)
            {
                await GeneratePrecipitationView();
            }

            StripeLoadingIndicatorVisible = false;
            StateHasChanged();

            Logger!.LogInformation("Finished loading data");
        }
    }

    protected async Task<LocationAnomalySummary?> CalculateAnomaly(List<DataSubstitute> dataSubstitutes, ContainerAggregationFunctions function)
    {
        return await ClimateDataHelper.CalculateAnomaly(DataService!, DataSetDefinitions!, Location!, dataSubstitutes, function);
    }

    protected async Task<DataSet?> GetData(List<DataSubstitute> dataSubstitutes, ContainerAggregationFunctions function)
    {
        return await ClimateDataHelper.GetData(DataService!, DataSetDefinitions!, Location!, dataSubstitutes, function);
    }

    protected async Task HandleOnYearFilterChange(short year)
    {
        var yearAndFilter = new YearAndDataTypeFilter(year) { DataType = TemperatureDataType, UnitOfMeasure = UnitOfMeasure.DegreesCelsius };
        await OnYearFilterChange.InvokeAsync(yearAndFilter);
    }

    protected async Task HandleOnPrecipitationYearFilterChange(short year)
    {
        var yearAndFilter = new YearAndDataTypeFilter(year) { DataType = DataType.Precipitation, UnitOfMeasure = UnitOfMeasure.Millimetres };
        await OnYearFilterChange.InvokeAsync(yearAndFilter);
    }

    protected async Task TogglePrecipitation()
    {
        DisplayPrecipitation = !DisplayPrecipitation;

        if (DisplayPrecipitation && !PrecipitationAnomalyLoaded)
        {
            await GeneratePrecipitationView();
        }
    }

    protected async Task GeneratePrecipitationView()
    {
        var precipitationAnomaly = await CalculateAnomaly([new DataSubstitute { DataType = DataType.Precipitation }], ContainerAggregationFunctions.Sum);
        if (precipitationAnomaly != null)
        {
            PrecipitationAnomalyLoaded = true;
            PrecipitationAnomaly = precipitationAnomaly.CalculatedAnomaly;
            PrecipitationLocationMean = precipitationAnomaly.DataSet?.DataRecords.Average(x => x.Value);
            PrecipitationAnomalyRecords = precipitationAnomaly.AnomalyRecords;
            PrecipitationAnomalyContent = Location == null
                    ? null
                    : builder =>
                    {
                        builder.OpenComponent(0, typeof(PrecipitationAnomalyInfo));
                        builder.AddAttribute(1, "Location", Location);
                        builder.AddAttribute(2, "CalculatedAnomaly", precipitationAnomaly.CalculatedAnomaly);
                        builder.CloseComponent();
                    };
        }
        else
        {
            PrecipitationAnomalyLoaded = false;
            PrecipitationAnomaly = null;
            PrecipitationLocationMean = null;
            PrecipitationAnomalyRecords = null;
            PrecipitationAnomalyContent = null;
        }
    }
}
