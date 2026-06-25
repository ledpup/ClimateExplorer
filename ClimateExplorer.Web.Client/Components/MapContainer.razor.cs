namespace ClimateExplorer.Web.Client.Components;

using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class MapContainer
{
    private static readonly string[] MarkerOptionLabels = ["negative", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "null"];

    private readonly SemaphoreSlim markerOptionsLock = new(1, 1);
    private readonly SemaphoreSlim mapLifecycleLock = new(1, 1);
    private Map? map;
    private MapOptions? mapOptions;
    private bool mainTileLayerCreated = false;
    private int? lastExpandedZoom;
    private int? lastCollapsedZoom;
    private LatLng? lastExpandedCentre = null;
    private LatLng? lastCollapsedCentre = null;
    private Core.Model.Location? internalLocation;
    private List<Guid> markersAdded = new();
    private Dictionary<string, MarkerOptions> markerOptions = new();
    private bool mapRerendering = false;
    private bool mapTransitionInProgress = false;
    private int mapRenderVersion = 0;

    [Parameter]
    public IEnumerable<Core.Model.Location>? Locations { get; set; }

    [Parameter]
    public EventCallback<Guid> OnLocationChange { get; set; }

    [Parameter]
    public Core.Model.Location? Location { get; set; }

    [Inject]
    public LayerFactory? LayerFactory { get; init; }

    [Inject]
    public IIconFactory? IconFactory { get; init; }

    [Inject]
    public IJSRuntime? JsRuntime { get; init; }

    [Inject]
    public ILogger<MapContainer>? Logger { get; init; }

    [Inject]
    public ICurrentDeviceService? CurrentDeviceService { get; set; }

    private bool IsMapExpanded { get; set; } = false;

    private bool? IsMobileDevice { get; set; }

    public async Task CreateMapMarkers()
    {
        if (mapTransitionInProgress || Locations == null || mapOptions == null || map == null || this.IconFactory == null || this.LayerFactory == null || JsRuntime == null)
        {
            return;
        }

        await mapLifecycleLock.WaitAsync();
        try
        {
            if (mapTransitionInProgress || Locations == null || mapOptions == null || map == null || this.IconFactory == null || this.LayerFactory == null || JsRuntime == null)
            {
                return;
            }

            var markerMap = map;
            var markerMapOptions = mapOptions;
            var markerMapVersion = mapRenderVersion;

            Logger!.LogInformation("Creating map markers");

            await CreateMarkerOptions();

            if (IsStaleMapMarkerCreation(markerMap, markerMapOptions, markerMapVersion))
            {
                return;
            }

            var bounds = await markerMap.GetBounds();

            if (bounds is null || IsStaleMapMarkerCreation(markerMap, markerMapOptions, markerMapVersion))
            {
                return;
            }

            int added = 0;
            foreach (var location in Locations)
            {
                if (markersAdded.Contains(location.Id))
                {
                    continue;
                }

                var lat = location.Coordinates.Latitude;
                var lng = location.Coordinates.Longitude;

                if (bounds is not null)
                {
                    bool latInRange = IsLatBetween(lat, bounds.SouthWest!.Lat, bounds.NorthEast!.Lat);
                    bool lngInRange = IsLngBetween(lng, bounds.SouthWest!.Lng, bounds.NorthEast!.Lng);

                    if (!latInRange || !lngInRange)
                    {
                        continue;
                    }
                }

                var label = GetMarkerOptionLabel(location);

                if (!markerOptions.TryGetValue(label, out var markerOption))
                {
                    Logger!.LogWarning(
                        "No marker option found for label {Label}. Marker option count is {MarkerOptionCount}.",
                        label,
                        markerOptions.Count);

                    continue;
                }

                if (IsStaleMapMarkerCreation(markerMap, markerMapOptions, markerMapVersion))
                {
                    return;
                }

                var marker = await this.LayerFactory.CreateMarkerAndAddToMap(new LatLng(lat, lng, location.Coordinates.Elevation ?? 0), markerMap, markerOption);

                if (IsStaleMapMarkerCreation(markerMap, markerMapOptions, markerMapVersion))
                {
                    return;
                }

                await marker.BindTooltip(location.FullTitle!);
                await marker.OnClick(async (MouseEvent mouseEvent) => await HandleMapMouseEvent(mouseEvent));

                markersAdded.Add(location.Id);

                added++;
            }

            Logger!.LogInformation($"Created {added} markers.");

            Logger!.LogInformation("Created map markers");
        }
        finally
        {
            mapLifecycleLock.Release();
        }
    }

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

            if (internalLocation is not null)
            {
                if (IsMapExpanded && map is not null)
                {
                    mapRerendering = true;
                    var selectedLocationCentre = new LatLng(internalLocation.Coordinates.Latitude, internalLocation.Coordinates.Longitude);
                    await CollapseMap(selectedLocationCentre);
                }
                else
                {
                    await ScrollToPoint(internalLocation.Coordinates);
                }
            }
        }

        await CreateVisibleMapMarkers();

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        IsMobileDevice ??= await CurrentDeviceService!.Mobile();
    }

    private static bool IsLatBetween(double value, double a, double b)
    {
        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        return value >= min && value <= max;
    }

    private static bool IsLngBetween(double lng, double west, double east)
    {
        // Normal case: west <= east
        if (west <= east)
        {
            return lng >= west && lng <= east;
        }

        // Bounds cross the dateline (e.g. west=170, east=-170)
        return lng >= west || lng <= east;
    }

    private static string GetMarkerOptionLabel(Core.Model.Location location)
    {
        if (location.HeatingScore == null)
        {
            return "null";
        }

        return location.HeatingScore.Value < 0
            ? "negative"
            : location.HeatingScore.Value.ToString();
    }

    private bool IsStaleMapMarkerCreation(Map markerMap, MapOptions markerMapOptions, int markerMapVersion)
    {
        return mapTransitionInProgress ||
            map != markerMap ||
            mapOptions != markerMapOptions ||
            mapRenderVersion != markerMapVersion;
    }

    private async Task AfterMapRender()
    {
        await CreateMarkerOptions();
        await CreateMainTileLayer();
        if (mapRerendering)
        {
            await ScrollToPoint(internalLocation!.Coordinates);
            mapRerendering = false;
        }

        await CreateMapMarkers();
        await map!.OnMoveEnd(async (moveEvent) => await HandleMoveEnd(moveEvent));
    }

    private async Task CreateVisibleMapMarkers()
    {
        if (map is null)
        {
            return;
        }

        await CreateMapMarkers();
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

    private async Task CreateMarkerOptions()
    {
        if (markerOptions.Count == MarkerOptionLabels.Length)
        {
            return;
        }

        await markerOptionsLock.WaitAsync();
        try
        {
            if (markerOptions.Count == MarkerOptionLabels.Length)
            {
                return;
            }

            var options = new Dictionary<string, MarkerOptions>(MarkerOptionLabels.Length);
            foreach (var label in MarkerOptionLabels)
            {
                var iconOptions = new IconOptions
                {
                    IconUrl = $"/images/map-markers/{label}.png",
                    IconSize = new Point(48, 48),
                    IconAnchor = new Point(23, 47),
                };

                options.Add(
                    label,
                    new MarkerOptions
                {
                    Alt = label,
                    Opacity = 0.75,
                    Draggable = false,
                    IconRef = await this.IconFactory!.Create(iconOptions),
                });
            }

            markerOptions = options;
        }
        finally
        {
            markerOptionsLock.Release();
        }
    }

    private async Task HandleMapMouseEvent(MouseEvent mouseEvent)
    {
        Logger!.LogInformation("HandleMapMouseEvent()");

        var lat = Math.Round(mouseEvent.LatLng!.Lat, 1);
        var lng = Math.Round(mouseEvent.LatLng.Lng, 1);
        var newLocation = Locations!.Single(x => Math.Round(x.Coordinates.Latitude, 1) == lat && Math.Round(x.Coordinates.Longitude, 1) == lng);
        lastExpandedCentre = new LatLng(newLocation.Coordinates.Latitude, newLocation.Coordinates.Longitude);
        await OnLocationChange.InvokeAsync(newLocation.Id);
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
            mapRenderVersion++;
            markersAdded = []; // Force re-creation of markers
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
                    Zoom = restoredZoom ?? currentZoom ?? 8,
                    Dragging = !(bool)IsMobileDevice! || IsMapExpanded,
                };
        }

        Logger!.LogInformation("Map options initialised");
    }

    private async Task HandleMoveEnd(MoveEvent moveEvent)
    {
        await CreateMapMarkers();
    }
}
