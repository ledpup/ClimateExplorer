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
    private Collapsible? suggestedChartsCollapsible;
    private InfoPanel? siteOverviewPanel;
    private LocationInfo? locationInfoComponent;

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

    private bool LocationChangeEventOccurring { get; set; } = false;

    private bool LocationDictionaryIncludesAllLocations { get; set; }

    private Task? LocationDictionaryLoadTask { get; set; }

    public override void Dispose()
    {
        SiteOverviewService!.ShowRequested -= HandleShowRequested;
        base.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        // Check to see if a named location is being requested. That will look like /location/<location-name>
        Location = await GetLocationFromPath();

        if (Location is not null)
        {
            LocationString = Location?.Id.ToString();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Handles named URLs when the Index component is reused across navigations
        // (OnInitializedAsync does not re-run in that case). LocationDictionary is null
        // on first load; the OnAfterRenderAsync first-load path covers that scenario.
        if (LocationDictionary is not null && !string.IsNullOrEmpty(LocationString) && !Guid.TryParse(LocationString, out _))
        {
            var name = LocationString.TrimEnd('/').ToLower();
            var newLocation = LocationDictionary.Values.FirstOrDefault(x => x.UrlReadyName() == name);
            newLocation ??= await DataService!.GetLocationByPath(name);
            if (newLocation is not null && newLocation.Id != Location?.Id)
            {
                AddLocationToDictionary(newLocation);
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
            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out _);
            var dataSetDefinitionsTask = DataService!.GetDataSetDefinitions(includeLargeLocationIds: csd);
            var regionsTask = DataService!.GetRegions();

            Task<IEnumerable<Location>>? locationsTask = csd
                ? DataService!.GetLocations(false)
                : null;

            if (locationsTask is null)
            {
                await Task.WhenAll(dataSetDefinitionsTask, regionsTask);
            }
            else
            {
                await Task.WhenAll(dataSetDefinitionsTask, locationsTask, regionsTask);
            }

            DataSetDefinitions = [.. await dataSetDefinitionsTask];
            Regions = [.. await regionsTask];

            if (locationsTask is not null)
            {
                LocationDictionary = (await locationsTask).ToDictionary(x => x.Id, x => x);
                LocationDictionaryIncludesAllLocations = true;
            }

            // If there is no "csd" query string parameter, set up default charts for the location
            var routeSegment = uri.Segments.Length > 2 ? uri.Segments[2].TrimEnd('/') : null;
            var isNamedUrl = routeSegment != null && !Guid.TryParse(routeSegment, out _);

            if (isNamedUrl)
            {
                Location ??= LocationDictionary?.Values.FirstOrDefault(x => x.UrlReadyName() == routeSegment!.ToLower())
                    ?? await DataService!.GetLocationByPath(routeSegment!.ToLower());
            }
            else
            {
                // Location may have been set in OnInitializedAsync.
                // Fall back to local storage; use Hobart as the final default.
                Location ??= csd
                    ? await GetLocation()
                    : await GetStoredOrDefaultLocation();
            }

            if (Location is not null)
            {
                AddLocationToDictionary(Location);
                await EnsureDataSetDefinitionsForLocation(Location.Id);
            }

            if (!csd && Location != null)
            {
                await LocalStorage!.SetItemAsync("lastLocationId", Location.Id.ToString());

                if (isNamedUrl)
                {
                    await NavigateTo($"/{PageName}/{Location.Id}", replace: true);
                }
                else if (!Guid.TryParse(routeSegment, out var routeLocationId) || routeLocationId != Location.Id)
                {
                    await NavigateTo($"/{PageName}/{Location.Id}");
                }

                _ = LoadFullLocationDictionaryInBackground();
            }

            StateHasChanged();
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
        if (uri.Segments.Length > 2)
        {
            var guidParsed = Guid.TryParse(uri.Segments[2], out Guid locationGuid);

            if (guidParsed)
            {
                location = await DataService!.GetLocationById(locationGuid);
            }
            else
            {
                var locationName = uri.Segments[2];

                location = await DataService!.GetLocationByPath(locationName.ToLower());

                if (location == null)
                {
                    NavManager!.NavigateTo("/error", true);
                }
            }
        }

        return location;
    }

    private async Task<Location?> GetLocation()
    {
        if (LocationDictionary is null)
        {
            return null;
        }

        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        var locationString = await LocalStorage!.GetItemAsync<string>("lastLocationId");
        var validGuid = Guid.TryParse(locationString, out Guid guid);
        Guid? locationId;
        if (validGuid && guid != Guid.Empty && LocationDictionary.ContainsKey(guid))
        {
            locationId = guid;
        }
        else
        {
            locationId = Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395");
        }

        // Override location with the one in csd, if there is a csd
        var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
        if (csd)
        {
            // Going to assume that the first chart is the primary location
            locationId = GetLocationFromCsd(csdSpecifier);
        }

        if (locationId is not null)
        {
            var location = LocationDictionary[locationId.Value];

            return location;
        }

        return null;
    }

    private async Task<Location?> GetStoredOrDefaultLocation()
    {
        var locationString = await LocalStorage!.GetItemAsync<string>("lastLocationId");
        var validGuid = Guid.TryParse(locationString, out Guid guid);

        if (validGuid && guid != Guid.Empty)
        {
            var storedLocation = await DataService!.GetLocationById(guid);
            if (storedLocation is not null)
            {
                return storedLocation;
            }
        }

        return await DataService!.GetLocationById(Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395"));
    }

    private void AddLocationToDictionary(Location location)
    {
        LocationDictionary ??= [];
        LocationDictionary[location.Id] = location;
    }

    private async Task EnsureFullLocationDictionaryLoaded()
    {
        if (LocationDictionaryIncludesAllLocations)
        {
            return;
        }

        try
        {
            LocationDictionaryLoadTask ??= LoadFullLocationDictionary();
            await LocationDictionaryLoadTask;
        }
        catch
        {
            LocationDictionaryLoadTask = null;
            throw;
        }
    }

    private async Task LoadFullLocationDictionary()
    {
        var locations = await DataService!.GetLocations(false);
        LocationDictionary = locations.ToDictionary(x => x.Id, x => x);
        LocationDictionaryIncludesAllLocations = true;

        if (Location is not null &&
            LocationDictionary.TryGetValue(Location.Id, out var hydratedLocation) &&
            (Location.WarmingAnomaly == null || Location.RecordHigh == null))
        {
            Location = hydratedLocation;
        }

        if (Location is not null)
        {
            LocationDictionary[Location.Id] = Location;
        }
    }

    private async Task LoadFullLocationDictionaryInBackground()
    {
        try
        {
            await EnsureFullLocationDictionaryLoaded();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger!.LogError(ex, "Unable to load locations in the background.");
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
        await EnsureFullLocationDictionaryLoaded();
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

        LocationDictionary ??= [];
        if (!LocationDictionary.TryGetValue(newValue, out Location? value))
        {
            value = await DataService!.GetLocationById(newValue);
            if (value is null)
            {
                Logger!.LogError($"{newValue} doesn't exist in the list of locations. Exiting SelectedLocationChangedInternal()");
                return;
            }

            AddLocationToDictionary(value);
        }

        await EnsureDataSetDefinitionsForLocation(newValue);

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
