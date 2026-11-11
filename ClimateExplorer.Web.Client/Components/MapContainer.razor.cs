namespace ClimateExplorer.Web.Client.Components;

using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Map;
using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class MapContainer : IAsyncDisposable
{
    private readonly SemaphoreSlim mapLifecycleLock = new(1, 1);
    private Map? map;
    private MapOptions? mapOptions;
    private bool mainTileLayerCreated = false;
    private int? lastExpandedZoom;
    private int? lastCollapsedZoom;
    private LatLng? lastExpandedCentre = null;
    private LatLng? lastCollapsedCentre = null;
    private Core.Model.Location? internalLocation;
    private bool mapRerendering = false;
    private bool mapTransitionInProgress = false;

    private IJSObjectReference? module;
    private DotNetObjectReference<MapContainer>? dotNetRef;

    // The JS marker layer (MapContainer.razor.js), the map it draws on, and what it was last given.
    private IJSObjectReference? markerLayer;
    private Map? markerLayerMap;
    private IEnumerable<Core.Model.Location>? markerLayerLocations;
    private Guid? markerLayerSelectedId;

    private IEnumerable<Core.Model.Location>? markersBuiltFor;
    private MapMarker[] markers = [];

    [Parameter]
    public IEnumerable<Core.Model.Location>? Locations { get; set; }

    [Parameter]
    public EventCallback<Guid> OnLocationChange { get; set; }

    [Parameter]
    public Core.Model.Location? Location { get; set; }

    [Inject]
    public LayerFactory? LayerFactory { get; init; }

    [Inject]
    public IJSRuntime? JsRuntime { get; init; }

    [Inject]
    public ILogger<MapContainer>? Logger { get; init; }

    [Inject]
    public ICurrentDeviceService? CurrentDeviceService { get; set; }

    private bool IsMapExpanded { get; set; } = false;

    private bool? IsMobileDevice { get; set; }

    public async Task CreateMainTileLayer()
    {
        if (map == null)
        {
            return;
        }

        if (mainTileLayerCreated)
        {
            return;
        }

        Logger!.LogInformation("Creating main tile layer");

        // Create Tile Layer
        var tileLayerOptions =
            new TileLayerOptions()
                {
                    Attribution = "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors",
                };

        var mainTileLayer = await LayerFactory!.CreateTileLayerAndAddToMap("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", map!, tileLayerOptions);

        mainTileLayerCreated = true;

        Logger!.LogInformation("Created main tile layer");
    }

    [JSInvokable]
    public async Task OnMarkerClick(Guid locationId)
    {
        var location = Locations?.FirstOrDefault(x => x.Id == locationId);
        if (location is null)
        {
            return;
        }

        lastExpandedCentre = new LatLng(location.Coordinates.Latitude, location.Coordinates.Longitude);
        await OnLocationChange.InvokeAsync(location.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeMarkerLayer();

        if (module is not null)
        {
            await module.DisposeAsync();
        }

        dotNetRef?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsMobileDevice == null || JsRuntime == null)
        {
            return;
        }

        var locationChanged = internalLocation?.Id != Location?.Id;

        if (locationChanged)
        {
            internalLocation = Location;

            InitialiseMapOptions();

            if (internalLocation is not null && map is not null)
            {
                if (IsMapExpanded)
                {
                    mapRerendering = true;
                    var selectedLocationCentre = new LatLng(internalLocation.Coordinates.Latitude, internalLocation.Coordinates.Longitude);
                    await CollapseMap(selectedLocationCentre);
                }
                else
                {
                    await ScrollToPoint(internalLocation.Coordinates);
                }

                lastExpandedCentre = new LatLng(internalLocation.Coordinates.Latitude, internalLocation.Coordinates.Longitude);
            }
        }

        await UpdateMarkerLayer();

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        IsMobileDevice ??= await CurrentDeviceService!.Mobile();
    }

    private static string GetMarkerIconLabel(Core.Model.Location location)
    {
        if (location.HeatingScore == null)
        {
            return "null";
        }

        return location.HeatingScore.Value < 0
            ? "negative"
            : location.HeatingScore.Value.ToString();
    }

    private async Task AfterMapRender()
    {
        await CreateMainTileLayer();
        if (mapRerendering)
        {
            await ScrollToPoint(internalLocation!.Coordinates);
            mapRerendering = false;
        }

        await UpdateMarkerLayer();
    }

    /// <summary>
    /// Creates the JS marker layer for the current map, or brings it up to date with the locations and the
    /// selected location. The layer itself adds and removes markers as the map moves.
    /// </summary>
    private async Task UpdateMarkerLayer()
    {
        if (mapTransitionInProgress || map is null || Locations is null || JsRuntime is null)
        {
            return;
        }

        await mapLifecycleLock.WaitAsync();
        try
        {
            var currentMap = map;
            var locations = Locations;
            if (mapTransitionInProgress || currentMap is null || locations is null)
            {
                return;
            }

            var selectedId = internalLocation?.Id;

            if (markerLayer is not null && markerLayerMap == currentMap)
            {
                if (!ReferenceEquals(markerLayerLocations, locations))
                {
                    markerLayerLocations = locations;
                    markerLayerSelectedId = selectedId;
                    await markerLayer.InvokeVoidAsync("setLocations", GetMarkers(locations), selectedId);
                }
                else if (markerLayerSelectedId != selectedId)
                {
                    markerLayerSelectedId = selectedId;
                    await markerLayer.InvokeVoidAsync("setSelected", selectedId);
                }

                return;
            }

            // The map is new (first render, or the map was expanded or collapsed), so the old layer went with the old map.
            await DisposeMarkerLayer();

            module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/MapContainer.razor.js");
            dotNetRef ??= DotNetObjectReference.Create(this);

            markerLayer = await module.InvokeAsync<IJSObjectReference>(
                "createMarkerLayer",
                currentMap.MapReference,
                dotNetRef,
                GetMarkers(locations),
                selectedId);
            markerLayerMap = currentMap;
            markerLayerLocations = locations;
            markerLayerSelectedId = selectedId;

            Logger!.LogInformation("Created map marker layer for {MarkerCount} locations", markers.Length);
        }
        catch (JSDisconnectedException)
        {
            // The circuit was torn down while a JS interop call was in flight (e.g. the user
            // navigated away). Drawing markers is moot at that point, so ignore it.
        }
        finally
        {
            mapLifecycleLock.Release();
        }
    }

    private MapMarker[] GetMarkers(IEnumerable<Core.Model.Location> locations)
    {
        if (ReferenceEquals(markersBuiltFor, locations))
        {
            return markers;
        }

        var locationList = locations.ToList();
        var minimumZooms = MapMarkerZoomTiers.Calculate(locationList);

        markers = [.. locationList
            .DistinctBy(x => x.Id)
            .Select(x => new MapMarker(
                x.Id,
                x.Coordinates.Latitude,
                x.Coordinates.Longitude,
                GetMarkerIconLabel(x),
                x.FullTitle,
                minimumZooms[x.Id]))];
        markersBuiltFor = locations;

        return markers;
    }

    private async Task DisposeMarkerLayer()
    {
        if (markerLayer is null)
        {
            return;
        }

        var layer = markerLayer;
        markerLayer = null;
        markerLayerMap = null;
        markerLayerLocations = null;
        markerLayerSelectedId = null;

        await layer.InvokeVoidIfConnectedAsync("dispose");
        await layer.DisposeAsync();
    }

    private async Task ScrollToPoint(Coordinates point)
    {
        if (map == null)
        {
            return;
        }

        var latlng = new LatLng(point.Latitude, point.Longitude);
        await map.PanTo(latlng);
    }

    private async Task ExpandMap()
    {
        if (IsMapExpanded)
        {
            return;
        }

        // Capture the current viewport before removing the map from the render tree.
        var currentZoom = await map!.GetZoom();
        var currentCentre = await map.GetCenter();

        lastCollapsedZoom = currentZoom;
        lastCollapsedCentre = currentCentre;

        await SetMapExpansion(true, currentZoom, currentCentre);
    }

    private Task CollapseMap()
    {
        return CollapseMap(null);
    }

    private async Task CollapseMap(LatLng? targetCollapsedCentre)
    {
        if (!IsMapExpanded)
        {
            return;
        }

        // Capture the current viewport before removing the map from the render tree.
        var currentZoom = await map!.GetZoom();
        var currentCentre = await map.GetCenter();

        lastExpandedZoom = currentZoom;
        lastExpandedCentre = targetCollapsedCentre ?? currentCentre;

        await SetMapExpansion(false, currentZoom, currentCentre, targetCollapsedCentre);
    }

    private async Task SetMapExpansion(bool expanded, int currentZoom, LatLng currentCentre, LatLng? targetCentreOverride = null)
    {
        // Null mapOptions only after all map calls are complete. Setting mapOptions = null removes the Map
        // component from the render tree, which Blazor can process during any subsequent await,
        // disposing the internal jsObjectReference and causing ArgumentNullException on map calls.
        await mapLifecycleLock.WaitAsync();
        try
        {
            mapTransitionInProgress = true;

            // The markers belong to the map being removed; AfterMapRender creates a layer on the new one.
            await DisposeMarkerLayer();

            mainTileLayerCreated = false;
            mapOptions = null;
            map = null;

            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            await JsRuntime!.InvokeVoidAsync("setMapExpansion", expanded);
            IsMapExpanded = expanded;

            InitialiseMapOptions(currentZoom, currentCentre, targetCentreOverride);
            mapTransitionInProgress = false;
        }
        finally
        {
            if (mapTransitionInProgress)
            {
                mapTransitionInProgress = false;
            }

            mapLifecycleLock.Release();
        }
    }

    private void InitialiseMapOptions(int? currentZoom = null, LatLng? currentCentre = null, LatLng? targetCentreOverride = null)
    {
        Logger!.LogInformation("Initialising map options");

        Logger!.LogDebug($"IsMapExpanded = {IsMapExpanded}");

        if (mapOptions == null && internalLocation is not null)
        {
            var centre = new LatLng(internalLocation.Coordinates.Latitude, internalLocation.Coordinates.Longitude);
            var restoredZoom = IsMapExpanded ? lastExpandedZoom : lastCollapsedZoom;
            var restoredCentre = IsMapExpanded ? lastExpandedCentre : lastCollapsedCentre;
            mapOptions = new MapOptions()
                {
                    Center = targetCentreOverride
                        ?? (restoredZoom.HasValue && restoredCentre is not null ? restoredCentre : currentCentre)
                        ?? centre,
                    Zoom = restoredZoom ?? currentZoom ?? 7,
                    Dragging = !(bool)IsMobileDevice! || IsMapExpanded,
                };
        }

        Logger!.LogInformation("Map options initialised");
    }

    /// <summary>
    /// One location as the JS marker layer needs it. Serialised camelCase.
    /// </summary>
    private sealed record MapMarker(Guid Id, double Lat, double Lng, string Label, string Title, int MinZoom);
}
