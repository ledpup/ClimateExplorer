namespace ClimateExplorer.Web.Client.Pages;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Components.Location;
using ClimateExplorer.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

public partial class Index : ChartablePage
{
    private const string DefaultLocationId = "aed87aa0-1d0c-44aa-8561-cde0fc936395";

    private Collapsible? suggestedChartsCollapsible;
    private InfoPanel? siteOverviewPanel;
    private LocationDashboard? locationInfoComponent;

    public Index()
    {
        PageName = "location";
    }

    [Parameter]
    public string? LocationString { get; set; }

    protected override string PageTitle
    {
        get
        {
            var title = Location == null ? $"ClimateExplorer - Local long-term climate trends" : $"ClimateExplorer - {Location.FullTitle}";

            return title;
        }
    }

    protected override string PageDescription
    {
        get
        {
            return Location == null
                ? "ClimateExplorer helps you understand climate change trends where you live. Explore long-term temperature and precipitation data for thousands of locations worldwide."
                : $"Explore long-term climate trends for {Location.FullTitle}. View temperature records, warming anomaly, heating score, and climate stripes.";
        }
    }

    protected override string PageUrl
    {
        get
        {
            return Location == null ? $"https://climateexplorer.net" : $"https://climateexplorer.net/location/{Location.UrlReadyName()}";
        }
    }

    [Inject]
    private Blazored.LocalStorage.ILocalStorageService? LocalStorage { get; set; }

    [Inject]
    private ISiteOverviewService? SiteOverviewService { get; set; }

    private ChangeLocation? ChangeLocationModal { get; set; }
    private string? BrowserLocationErrorMessage { get; set; }
    private Location? Location { get; set; }

    private Location? PreviousLocation { get; set; }

    private IEnumerable<Location>? MapLocations
    {
        get
        {
            if (LocationDictionary is not null)
            {
                return LocationDictionary.Values;
            }

            return Location is null ? null : [Location];
        }
    }

    private bool LocationChangeEventOccurring { get; set; } = false;

    private CancellationTokenSource? DeferredLocationDictionaryLoadCts { get; set; }

    private Task? LocationDictionaryLoadTask { get; set; }

    public override void Dispose()
    {
        SiteOverviewService!.ShowRequested -= HandleShowRequested;
        DeferredLocationDictionaryLoadCts?.Cancel();
        DeferredLocationDictionaryLoadCts?.Dispose();
        base.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        // Check to see if a named location is being requested. That will look like /location/<location-name>
        Location = await GetLocationFromPath();

        if (Location is not null)
        {
            LocationString = Location.Id.ToString();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Handles named URLs when the Index component is reused across navigations
        // (OnInitializedAsync does not re-run in that case).
        if (LocationDictionary is not null && !string.IsNullOrEmpty(LocationString) && !Guid.TryParse(LocationString, out _))
        {
            var name = LocationString.TrimEnd('/').ToLower();
            var newLocation = LocationDictionary.Values.FirstOrDefault(x => x.UrlReadyName() == name);
            if (newLocation is not null && newLocation.Id != Location?.Id)
            {
                await NavigateTo($"/{PageName}/{newLocation.Id}", replace: true);
                await SelectedLocationChangedInternal(newLocation.Id);
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            SiteOverviewService!.ShowRequested += HandleShowRequested;
        }

        if (DataSetDefinitions is null)
        {
            var dataSetDefinitionsTask = DataService!.GetDataSetDefinitions();
            var regionsTask = DataService!.GetRegions();

            await Task.WhenAll(dataSetDefinitionsTask, regionsTask);

            DataSetDefinitions = [.. await dataSetDefinitionsTask];
            Regions = [.. await regionsTask];

            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out _);

            if (csd)
            {
                await EnsureLocationDictionaryLoadedAsync();
            }

            Location ??= await GetLocation();
            if (Location is not null)
            {
                LocationString = Location.Id.ToString();
                await LocalStorage!.SetItemAsync("lastLocationId", Location.Id.ToString());
            }

            if (!csd && Location != null)
            {
                var routeSegment = uri.Segments.Length > 2 ? uri.Segments[2].TrimEnd('/') : null;
                var isNamedUrl = routeSegment != null && !Guid.TryParse(routeSegment, out _);

                if (isNamedUrl)
                {
                    await NavigateTo($"/{PageName}/{Location.Id}", replace: true);
                }
                else if (routeSegment is null)
                {
                    await NavigateTo($"/{PageName}/{Location.Id}");
                }
            }

            StateHasChanged();
        }
        else if (LocationDictionary is null && Location is not null)
        {
            StartDeferredLocationDictionaryLoad();
        }

        // Handle location change event (coming from the map or the Change Location modal)
        if (LocationChangeEventOccurring && LocationString is not null)
        {
            LocationChangeEventOccurring = false;
            var guidParsed = Guid.TryParse(LocationString, out Guid locationGuid);
            if (guidParsed)
            {
                await SelectedLocationChangedInternal(locationGuid);
                StateHasChanged();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task<Location?> GetLocationFromPath()
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        Location? location = null;
        if (uri.Segments.Length <= 2)
        {
            return location;
        }

        var routeSegment = uri.Segments[2].TrimEnd('/');
        if (Guid.TryParse(routeSegment, out Guid locationGuid))
        {
            location = await DataService!.GetLocationById(locationGuid);
        }
        else
        {
            location = await DataService!.GetLocationByPath(routeSegment.ToLower());
        }

        if (location is null)
        {
            NavManager!.NavigateTo("/error", true);
        }

        return location;
    }

    private async Task<Location?> GetLocation()
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        var locationString = await LocalStorage!.GetItemAsync<string>("lastLocationId");
        var validGuid = Guid.TryParse(locationString, out Guid guid);
        Guid? locationId = null;

        if (validGuid && guid != Guid.Empty)
        {
            locationId = guid;
        }

        var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
        if (csd)
        {
            await EnsureLocationDictionaryLoadedAsync();
            locationId = GetLocationFromCsd(csdSpecifier);
        }

        if (locationId is not null)
        {
            var location = await DataService!.GetLocationById(locationId.Value);
            if (location is not null)
            {
                return location;
            }
        }

        return await DataService!.GetLocationById(Guid.Parse(DefaultLocationId));
    }

    private void StartDeferredLocationDictionaryLoad()
    {
        if (LocationDictionary is not null || DeferredLocationDictionaryLoadCts is not null)
        {
            return;
        }

        DeferredLocationDictionaryLoadCts = new CancellationTokenSource();
        _ = LoadLocationDictionaryAfterDelayAsync(DeferredLocationDictionaryLoadCts.Token);
    }

    private async Task LoadLocationDictionaryAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);

            await InvokeAsync(
                async () =>
                {
                    await EnsureLocationDictionaryLoadedAsync();
                    StateHasChanged();
                });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task EnsureLocationDictionaryLoadedAsync()
    {
        if (LocationDictionary is not null)
        {
            return;
        }

        DeferredLocationDictionaryLoadCts?.Cancel();
        LocationDictionaryLoadTask ??= LoadLocationDictionaryAsync();

        await LocationDictionaryLoadTask;
    }

    private async Task LoadLocationDictionaryAsync()
    {
        LocationDictionary = (await DataService!.GetLocations(false)).ToDictionary(x => x.Id, x => x);

        if (Location is not null)
        {
            LocationDictionary.TryAdd(Location.Id, Location);
        }
    }

    private async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await ChartView!.HandleOnYearFilterChange(yearAndDataTypeFilter);
    }

    private async Task OnOverviewShowHide(bool isOverviewVisible)
    {
        if (JsRuntime is not null)
        {
            await JsRuntime.InvokeVoidAsync("showOrHideMap", isOverviewVisible);
        }
    }

    private async Task ShowChangeLocationModal()
    {
        await EnsureLocationDictionaryLoadedAsync();
        await ChangeLocationModal!.Show();
    }

    private void HandleShowRequested()
    {
        if (siteOverviewPanel is not null)
        {
            _ = siteOverviewPanel.ShowAsync();
        }
    }

    private async Task SelectedLocationChanged(Guid locationId)
    {
        if (locationId == Guid.Empty)
        {
            return;
        }

        LocationChangeEventOccurring = true;

        await NavigateTo($"/{PageName}/" + locationId.ToString());
    }

    private async Task SelectedLocationChangedInternal(Guid newValue)
    {
        Logger!.LogInformation("SelectedLocationChangedInternal(): " + newValue);

        Location? value = null;
        if (LocationDictionary?.TryGetValue(newValue, out var dictionaryLocation) == true)
        {
            value = dictionaryLocation;
        }
        else if (Location?.Id == newValue)
        {
            value = Location;
        }
        else
        {
            value = await DataService!.GetLocationById(newValue);
        }

        if (value is null)
        {
            Logger!.LogError($"{newValue} doesn't exist in the list of locations. Exiting SelectedLocationChangedInternal()");
            return;
        }

        PreviousLocation = Location;
        Location = value;

        await LocalStorage!.SetItemAsync("lastLocationId", newValue.ToString());

        StateHasChanged();
    }

    private void ToggleSuggestedCharts()
    {
        suggestedChartsCollapsible?.CollapserOnClick();
    }

    private Task ShowRecordHighAsync() => locationInfoComponent?.ShowRecordHighAsync() ?? Task.CompletedTask;
}
