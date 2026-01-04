namespace ClimateExplorer.Web.Client.Pages;

using Blazorise.Snackbar;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Shared;
using ClimateExplorer.Web.Client.Shared.LocationComponents;
using ClimateExplorer.Web.UiModel;
using CurrentDevice;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class Index : ChartablePage
{
    private Collapsible? suggestedChartsCollapsible;

    public Index()
    {
        PageName = "location";
    }

    [Parameter]
    public string? LocationString { get; set; }

    public bool? IsMobileDevice { get; private set; }

    protected override string PageTitle
    {
        get
        {
            var title = Location == null ? $"Local long-term climate trends" : $"ClimateExplorer - {Location.FullTitle}";

            return title;
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
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    private ChangeLocation? ChangeLocationModal { get; set; }
    private MapContainer? MapContainer { get; set; }
    private string? BrowserLocationErrorMessage { get; set; }
    private Location? Location { get; set; }

    private bool LocationChangeEventOccurring { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        // Check to see if a named location is being requested. That will look like /location/<location-name>
        Location = await GetNamedLocation();

        if (Location is not null)
        {
            LocationString = Location?.Id.ToString();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsMobileDevice is null)
        {
            IsMobileDevice = await CurrentDeviceService!.Mobile();
        }

        if (LocationDictionary is null)
        {
            var dataSetDefinitionsTask = DataService!.GetDataSetDefinitions();
            var locationsTask = DataService!.GetLocations(false);
            var regionsTask = DataService!.GetRegions();

            await Task.WhenAll(dataSetDefinitionsTask, locationsTask, regionsTask);

            DataSetDefinitions = (await dataSetDefinitionsTask).ToList();
            LocationDictionary = (await locationsTask).ToDictionary(x => x.Id, x => x);
            Regions = (await regionsTask).ToList();

            // Location may have been set in OnInitializedAsync
            if (Location is null)
            {
                // Get location from local storage. If not in local storage, use Hobart as default.
                // If we have URL that is a csd, override with the location specified in the csd.
                Location = await GetLocation();
            }

            // If there is no "csd" query string parameter, set up default charts for the location
            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
            if (!csd && Location != null)
            {
                await SelectedLocationChanged(Location.Id);
            }

            StateHasChanged();
        }

        // Handle location change event (coming from the map or the Change Location modal)
        if (LocationChangeEventOccurring && LocationString is not null)
        {
            LocationChangeEventOccurring = false;
            var locationId = Guid.Parse(LocationString!);
            await SelectedLocationChangedInternal(locationId);
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task<Location?> GetNamedLocation()
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        Location? location = null;
        if (uri.Segments.Length > 2 && !Guid.TryParse(uri.Segments[2], out Guid locationGuid))
        {
            var locationName = uri.Segments[2];

            location = await DataService!.GetLocationByPath(locationName.ToLower());

            if (location == null)
            {
                NavManager!.NavigateTo("/error", true);
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
        Guid? locationId = null;
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
            locationId = await ChartView!.UpdateUiStateBasedOnQueryString();
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
        await JsRuntime!.InvokeVoidAsync("showOrHideMap", isOverviewVisible);
    }

    private Task ShowChangeLocationModal()
    {
        return ChangeLocationModal!.Show();
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

        if (!LocationDictionary!.ContainsKey(newValue))
        {
            Logger!.LogError($"{newValue} doesn't exist in the list of locations. Exiting SelectedLocationChangedInternal()");
            return;
        }

        Location = LocationDictionary[newValue];

        await LocalStorage!.SetItemAsync("lastLocationId", newValue.ToString());

        StateHasChanged();
    }
}
