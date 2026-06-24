namespace ClimateExplorer.Web.Client.Components;

using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class MapContainer
{
    private static readonly string[] MarkerOptionLabels = ["negative", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "null"];

    private readonly SemaphoreSlim markerOptionsLock = new(1, 1);
    private Map? map;
    private MapOptions? mapOptions;
    private bool mainTileLayerCreated = false;
    private int lastExpandedZoom = 8;
    private int lastCollapsedZoom = 8;
    private LatLng? expandedCentre = null;
    private LatLng? collapsedCentre = null;
    private Core.Model.Location? internalLocation;
    private List<Guid> markersAdded = new();
    private Dictionary<string, MarkerOptions> markerOptions = new();
    private bool mapRerendering = false;

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
        if (Locations == null || mapOptions == null || map == null || this.IconFactory == null || this.LayerFactory == null || JsRuntime == null)
        {
            return;
        }

        Logger!.LogInformation("Creating map markers");

        await CreateMarkerOptions();

        var bounds = await map.GetBounds();

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

            var marker = await this.LayerFactory.CreateMarkerAndAddToMap(new LatLng(lat, lng, location.Coordinates.Elevation ?? 0), map, markerOption);

            await marker.BindTooltip(location.FullTitle!);
            await marker.OnClick(async (MouseEvent mouseEvent) => await HandleMapMouseEvent(mouseEvent));

            markersAdded.Add(location.Id);

            added++;
        }

        Logger!.LogInformation($"Created {added} markers.");

        Logger!.LogInformation("Created map markers");
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
                    await ToggleMapExpansion();
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
        await OnLocationChange.InvokeAsync(newLocation.Id);
    }

    private async Task ToggleMapExpansion()
    {
        // Capture map state before nulling mapOptions — setting mapOptions = null removes the Map
        // component from the render tree, which Blazor can process during any subsequent await,
        // disposing the internal jsObjectReference and causing ArgumentNullException on map calls.
        var zoom = await map!.GetZoom();
        if (IsMapExpanded)
        {
            lastExpandedZoom = zoom;
            expandedCentre = await map!.GetCenter();
        }
        else
        {
            lastCollapsedZoom = zoom;
            collapsedCentre = internalLocation is null ? null : new LatLng(internalLocation.Coordinates.Latitude, internalLocation.Coordinates.Longitude);
        }

        markersAdded = []; // Force re-creation of markers
        mainTileLayerCreated = false;
        mapOptions = null;

        await JsRuntime!.InvokeVoidAsync("toggleMapExpansion", null);
        IsMapExpanded = !IsMapExpanded;

        InitialiseMapOptions();
    }

    private void InitialiseMapOptions()
    {
        Logger!.LogInformation("Initialising map options");

        Logger!.LogDebug($"IsMapExpanded = {IsMapExpanded}");

        if (mapOptions == null && internalLocation is not null)
        {
            var centre = new LatLng(internalLocation.Coordinates.Latitude, internalLocation.Coordinates.Longitude);
            mapOptions = new MapOptions()
                {
                    Center = IsMapExpanded ? expandedCentre ?? centre : collapsedCentre ?? centre,
                    Zoom = IsMapExpanded ? lastExpandedZoom : lastCollapsedZoom,
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
