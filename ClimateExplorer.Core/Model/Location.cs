using ClimateExplorer.Core;
using GeoCoordinatePortable;
using System.Text.Json;

public class Location
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public Coordinates Coordinates { get; set; }
    public float? WarmingIndex { get; set; }
    public short? HeatingScore { get; set; }
    public List<LocationDistance>? NearbyLocations { get; set; }

    public static async Task<List<Location>> GetLocationsFromFile(string pathAndFileName)
    {
        var locationText = await File.ReadAllTextAsync(pathAndFileName);
        var locations = JsonSerializer.Deserialize<List<Location>>(locationText);
        return locations;
    }

    public static async Task<List<Location>> GetLocations(bool setNearbyLocations, string? folderName = null)
    {
        folderName = folderName ?? @"MetaData\Location";
        var locations = new List<Location>();
        var locationFiles = Directory.GetFiles(folderName).ToList();
        foreach (var file in locationFiles)
        {
            var locationsInFile = await GetLocationsFromFile(file);
            locations.AddRange(locationsInFile);
        }
        

        if (setNearbyLocations)
        {
            SetNearbyLocations(locations);
        }

        locations = locations.OrderBy(x => x.Name).ToList();

        return locations;
    }

    public static void SetNearbyLocations(List<Location>? locations)
    {
        Parallel.ForEach(locations, location =>
        {
            var distances = GetDistances(location, locations);

            location.NearbyLocations = distances.OrderBy(x => x.Distance).Take(10).ToList();
        });
    }

    public static List<LocationDistance> GetDistances(Location location, List<Location> locations)
    {
        var originCoord = new GeoCoordinate(location.Coordinates.Latitude, location.Coordinates.Longitude, location.Coordinates.Elevation ?? 0);
        return GetDistances(originCoord, locations.Where(x => x != location));
    }

    public static List<LocationDistance> GetDistances(GeoCoordinate geoCoordinate, IEnumerable<Location> locations)
    {
        var distances = new List<LocationDistance>();

        locations.ToList().ForEach(x =>
        {
            var destCoord = new GeoCoordinate(x.Coordinates.Latitude, x.Coordinates.Longitude, x.Coordinates.Elevation ?? 0);
            var distance = Math.Round(geoCoordinate.GetDistanceTo(destCoord) / 1000, 1); // GetDistanceTo is in metres. Convert to km
            var bearing = GeometryHelpers.GetRhumbBearingFromPointToPoint(geoCoordinate, destCoord);

            distances.Add(
                new LocationDistance
                {
                    LocationId = x.Id,
                    LocationName = x.Name,
                    Distance = distance,
                    BearingDegrees = bearing,
                    CompassRoseDirection = GeometryHelpers.GetCompassRoseDirectionName(bearing)
                });
        });

        return distances;
    }
}

public class LocationDistance
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; }
    public double Distance { get; set; }
    public double BearingDegrees { get; set; }
    public string CompassRoseDirection { get; set; }
}