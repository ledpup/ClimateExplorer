namespace ClimateExplorer.Web.Client.Components.Location;

using Blazorise;
using Blazorise.Components;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApiClient.Services;
using GeoCoordinatePortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class ChangeLocation
{
    private const int PageSize = 10;

    private Modal? modal;
    private Autocomplete<Location, Guid>? locationAutocomplete;
    private string? selectedText;
    private bool isModalOpen;
    private bool shouldFocusLocationSearch;

    [Inject]
    public IDataService? DataService { get; set; }

    [Parameter]
    public Dictionary<Guid, Location>? LocationDictionary { get; set; }

    [Parameter]
    public Location? SelectedLocation { get; set; }

    [Parameter]
    public string? BrowserLocationErrorMessage { get; set; }

    [Parameter]
    public EventCallback<Guid> OnLocationChange { get; set; }

    public IEnumerable<LocationDistance>? NearbyLocations { get; set; }

    [Inject]
    protected IJSRuntime? JsRuntime { get; set; }

    [Inject]
    protected ILogger<ChangeLocation>? Logger { get; set; }

    private Guid? InternalLocationId { get; set; }

    private int CurrentPage { get; set; } = 1;

    private int TotalPages => (int)Math.Ceiling((LocationDictionary?.Count ?? 0) / (double)PageSize);

    private bool NearbyLocationsLoading { get; set; }

    public async Task Show()
    {
        if (LocationDictionary is null)
        {
            return;
        }

        selectedText = null;
        shouldFocusLocationSearch = true;

        if (SelectedLocation is not null && NearbyLocations is null)
        {
            _ = LoadNearbyLocationsAsync(SelectedLocation.Id);
        }

        await modal!.Show();
    }

    public Task Hide()
    {
        return modal!.Hide();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (LocationDictionary is null)
        {
            return;
        }

        if (SelectedLocation is not null && SelectedLocation.Id != InternalLocationId)
        {
            InternalLocationId = SelectedLocation.Id;
            NearbyLocations = null;
            CurrentPage = 1;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (isModalOpen && shouldFocusLocationSearch)
        {
            await FocusLocationSearch();
        }
    }

    private async Task OnModalOpened()
    {
        isModalOpen = true;

        // Let the modal finish applying its own focus management before focusing the search input.
        await Task.Delay(50);
        await FocusLocationSearch();
    }

    private Task OnModalClosed()
    {
        isModalOpen = false;
        shouldFocusLocationSearch = false;

        return Task.CompletedTask;
    }

    private async Task FocusLocationSearch()
    {
        if (locationAutocomplete is null)
        {
            return;
        }

        shouldFocusLocationSearch = false;
        await locationAutocomplete.Focus(scrollToElement: false);
    }

    private async Task LoadNearbyLocationsAsync(Guid locationId, int page = 1)
    {
        NearbyLocationsLoading = true;
        StateHasChanged();

        var skip = (page - 1) * PageSize;
        NearbyLocations = await DataService!.GetNearbyLocations(locationId, take: PageSize, skip: skip);

        CurrentPage = page;

        NearbyLocationsLoading = false;
        StateHasChanged();
    }

    private Task OnNearbyLocationsPageChanged(int page) => LoadNearbyLocationsAsync(InternalLocationId!.Value, page);

    private async Task NearMeClicked()
    {
        if (JsRuntime == null)
        {
            Logger!.LogError("JsRuntime is required.");
            return;
        }

        var getLocationResult = await JsRuntime.InvokeAsync<GetLocationResult>("getLocation");

        BrowserLocationErrorMessage = null;
        if (getLocationResult.ErrorCode > 0)
        {
            BrowserLocationErrorMessage = "Unable to determine your location" + (!string.IsNullOrWhiteSpace(getLocationResult.ErrorMessage) ? $" ({getLocationResult.ErrorMessage})" : string.Empty);
            Logger!.LogError(BrowserLocationErrorMessage);
            return;
        }

        var geoCoord = new GeoCoordinate(getLocationResult.Latitude, getLocationResult.Longitude);

        var distances = Location.GetDistances(geoCoord, LocationDictionary!.Values!);
        var closestLocation = distances.OrderBy(x => x.Distance).First();

        await OnLocationChange.InvokeAsync(closestLocation.LocationId);
        await Hide();
    }

    private async Task FireOnLocationChange(Guid locationId)
    {
        if (locationId != Guid.Empty)
        {
            await OnLocationChange.InvokeAsync(locationId);
            await Hide();
        }
    }

    private async Task RandomLocationClicked()
    {
        var random = new Random();
        var randomLocation = random.Next(LocationDictionary!.Values.Count());
        await OnLocationChange.InvokeAsync(LocationDictionary!.Values.ToArray()[randomLocation].Id);
        await Hide();
    }

    private class GetLocationResult
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public float ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
