namespace ClimateExplorer.Web.Client.Components;

using ClimateExplorer.Core.Model;
using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class MapContainer
{
    private Map? map;
    private MapOptions? mapOptions;
    private bool mainTileLayerCreated = false;
    private int lastExpandedZoom = 8;
    private int lastCollapsedZoom = 8;
    private LatLng? expandedCentre = null;
    private LatLng? collapsedCentre = null;
    private Core.Model.Location? internalLocation;
    private List<Guid> markersAdded = new();
    private List<MarkerOptions> markerOptions = new();
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

            bool latInRange = IsLatBetween(lat, bounds.SouthWest!.Lat, bounds.NorthEast!.Lat);
            bool lngInRange = IsLngBetween(lng, bounds.SouthWest!.Lng, bounds.NorthEast!.Lng);

            if (!latInRange || !lngInRange)
            {
                continue;
            }

            var label = location.HeatingScore == null
                    ? "null"
                    : location.HeatingScore.Value < 0
                        ? "negative"
                        : location.HeatingScore.Value.ToString();

            var markerOption = markerOptions.Single(x => x.Alt == label);
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
        if (internalLocation?.Id == Location?.Id || IsMobileDevice == null || JsRuntime == null)
        {
            return;
        }

        internalLocation = Location;

        InitialiseMapOptions();

        if (IsMapExpanded)
        {
            mapRerendering = true;
            await ToggleMapExpansion();
        }
        else
        {
            await ScrollToPoint(internalLocation!.Coordinates);
        }

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
        if (markerOptions.Any())
        {
            return;
        }

        markerOptions = new List<MarkerOptions>();
        for (var i = -1; i < 11; i++)
        {
            var label = i == -1
                                ? "negative"
                                : i == 10
                                    ? "null"
                                    : i.ToString();
            var iconOptions = new IconOptions
                {
                    IconUrl = $"/images/map-markers/{label}.png",
                    IconSize = new Point(48, 48),
                    IconAnchor = new Point(23, 47),
                };

            markerOptions.Add(
                new MarkerOptions
                {
                    Alt = label,
                    Opacity = 0.75,
                    Draggable = false,
                    IconRef = await this.IconFactory!.Create(iconOptions),
                });
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
        markersAdded = new(); // Force re-creation of markers
        mainTileLayerCreated = false;
        mapOptions = null;

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
