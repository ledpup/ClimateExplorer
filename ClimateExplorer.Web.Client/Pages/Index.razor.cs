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

    private bool LocationChangeEventOccurring { get; set; } = false;

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
            var locationsTask = DataService!.GetLocations(false);
            var regionsTask = DataService!.GetRegions();

            await Task.WhenAll(dataSetDefinitionsTask, locationsTask, regionsTask);

            DataSetDefinitions = [.. await dataSetDefinitionsTask];
            LocationDictionary = (await locationsTask).ToDictionary(x => x.Id, x => x);
            Regions = [.. await regionsTask];

            // If there is no "csd" query string parameter, set up default charts for the location
            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            var routeSegment = uri.Segments.Length > 2 ? uri.Segments[2].TrimEnd('/') : null;
            var isNamedUrl = routeSegment != null && !Guid.TryParse(routeSegment, out _);

            if (isNamedUrl)
            {
                // Resolve from the just-loaded LocationDictionary. GetLocationByPath has no
                // client-side cache so it races with the bulk data load above; using the
                // dictionary avoids the race and bypasses the LocalStorage fallback entirely.
                Location = LocationDictionary!.Values.FirstOrDefault(x => x.UrlReadyName() == routeSegment!.ToLower());
            }
            else
            {
                // Location may have been set in OnInitializedAsync.
                // Fall back to local storage; use Hobart as the final default.
                Location ??= await GetLocation();
            }

            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
            if (!csd && Location != null)
            {
                if (isNamedUrl)
                {
                    await NavigateTo($"/{PageName}/{Location.Id}", replace: true);
                    await SelectedLocationChangedInternal(Location.Id);
                }
                else
                {
                    await SelectedLocationChanged(Location.Id);
                }
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

    private Task ShowChangeLocationModal()
    {
        return ChangeLocationModal!.Show();
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

        if (!LocationDictionary!.TryGetValue(newValue, out Location? value))
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
