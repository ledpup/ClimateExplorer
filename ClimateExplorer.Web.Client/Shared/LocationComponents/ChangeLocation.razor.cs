namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

using Blazorise;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApiClient.Services;
using GeoCoordinatePortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class ChangeLocation
{
    private Modal? modal;
    private string? selectedText;

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

    private bool NearbyLocationsLoading { get; set; } = true;

    public async Task Show()
    {
        selectedText = null;

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
            NearbyLocationsLoading = true;
        }
    }

    private async Task LoadNearbyLocationsAsync(Guid locationId)
    {
        NearbyLocations = await DataService!.GetNearbyLocations(locationId);
        NearbyLocationsLoading = false;
        StateHasChanged();
    }

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
