namespace ClimateExplorer.Web.Client.Services.Map;

using ClimateExplorer.Core.Model;

/// <summary>
/// Works out the lowest map zoom at which each location's marker shows, so markers never overlap. Locations
/// with longer records claim their space first: zoomed out, only they show, and zooming in spreads markers
/// apart so their neighbours appear. A marker shown at one zoom is shown at every higher zoom.
/// </summary>
public static class MapMarkerZoomTiers
{
    /// <summary>
    /// The visible part of the marker pin. MapContainer.razor.js draws the icon at 48 x 48 px, but the pin is
    /// narrower than its image.
    /// </summary>
    public const double MarkerWidthPixels = 36;

    public const double MarkerHeightPixels = 44;

    public const int MinimumZoom = 0;

    /// <summary>
    /// Markers that still overlap at this zoom (for example, two locations at the same coordinates) show from
    /// here on, overlapping.
    /// </summary>
    public const int MaximumZoom = 12;

    // Leaflet's world is 256 px wide at zoom 0, doubling with each zoom level.
    private const double WorldPixelsAtZoomZero = 256;

    // Web Mercator's latitude limit; the projection is infinite at the poles.
    private const double MaximumLatitude = 85.0511287798;

    public static Dictionary<Guid, int> Calculate(IEnumerable<Location> locations)
    {
        var markers = locations
            .DistinctBy(x => x.Id)
            .Select(x => new Marker(x.Id, Project(x.Coordinates), GetRecordLength(x)))
            .OrderByDescending(x => x.RecordLength)
            .ThenBy(x => x.Id)
            .ToList();

        var minimumZooms = new Dictionary<Guid, int>(markers.Count);
        var shown = new List<Marker>(markers.Count);

        for (var zoom = MinimumZoom; zoom < MaximumZoom && shown.Count < markers.Count; zoom++)
        {
            var scale = Math.Pow(2, zoom);
            var grid = new Dictionary<(int X, int Y), List<Marker>>();
            foreach (var marker in shown)
            {
                AddToGrid(grid, marker, scale);
            }

            foreach (var marker in markers)
            {
                if (minimumZooms.ContainsKey(marker.Id) || OverlapsShownMarker(grid, marker, scale))
                {
                    continue;
                }

                minimumZooms[marker.Id] = zoom;
                shown.Add(marker);
                AddToGrid(grid, marker, scale);
            }
        }

        foreach (var marker in markers)
        {
            minimumZooms.TryAdd(marker.Id, MaximumZoom);
        }

        return minimumZooms;
    }

    /// <summary>
    /// Years from the start of the location's temperature record to its last complete year. Zero when the
    /// record is too short for a trend (under 30 years) or hasn't been calculated.
    /// </summary>
    public static int GetRecordLength(Location location)
    {
        return location.LastYear is int lastYear && location.FullPeriodTemperatureTrend is { } trend
            ? lastYear - trend.FirstYear + 1
            : 0;
    }

    private static (double X, double Y) Project(Coordinates coordinates)
    {
        var latitude = Math.Clamp(coordinates.Latitude, -MaximumLatitude, MaximumLatitude);
        var sinLatitude = Math.Sin(latitude * Math.PI / 180);
        var x = (coordinates.Longitude + 180) / 360 * WorldPixelsAtZoomZero;
        var y = (0.5 - (Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI))) * WorldPixelsAtZoomZero;
        return (x, y);
    }

    private static (int X, int Y) GridCell(Marker marker, double scale)
    {
        return ((int)Math.Floor(marker.Pixel.X * scale / MarkerWidthPixels), (int)Math.Floor(marker.Pixel.Y * scale / MarkerHeightPixels));
    }

    private static void AddToGrid(Dictionary<(int X, int Y), List<Marker>> grid, Marker marker, double scale)
    {
        var cell = GridCell(marker, scale);
        if (!grid.TryGetValue(cell, out var markersInCell))
        {
            markersInCell = [];
            grid[cell] = markersInCell;
        }

        markersInCell.Add(marker);
    }

    // A grid cell is one marker wide and high, so any overlapping marker is in this cell or a neighbouring one.
    private static bool OverlapsShownMarker(Dictionary<(int X, int Y), List<Marker>> grid, Marker marker, double scale)
    {
        var (cellX, cellY) = GridCell(marker, scale);
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (!grid.TryGetValue((cellX + dx, cellY + dy), out var markersInCell))
                {
                    continue;
                }

                foreach (var other in markersInCell)
                {
                    if (Math.Abs(other.Pixel.X - marker.Pixel.X) * scale < MarkerWidthPixels
                        && Math.Abs(other.Pixel.Y - marker.Pixel.Y) * scale < MarkerHeightPixels)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private sealed record Marker(Guid Id, (double X, double Y) Pixel, int RecordLength);
}
