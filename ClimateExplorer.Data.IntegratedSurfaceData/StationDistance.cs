using GeoCoordinatePortable;

namespace ClimateExplorer.Data.IntegratedSurfaceData;

public class StationDistance
{
    public string? Id { get; set; }
    public double Distance { get; set; }

    public static List<StationDistance> GetDistances(Station station, List<Station> stations)
    {
        var originCoord = new GeoCoordinate(station.Coordinates.Latitude, station.Coordinates.Longitude, station.Coordinates.Elevation.HasValue ? station.Coordinates.Elevation.Value : 0);
        return GetDistances(originCoord, stations.Where(x => x != station));
    }

    public static List<StationDistance> GetDistances(GeoCoordinate geoCoordinate, IEnumerable<Station> stations)
    {
        var distances = new List<StationDistance>();

        stations.ToList().ForEach(x =>
        {
            var destCoord = new GeoCoordinate(x.Coordinates.Latitude, x.Coordinates.Longitude, x.Coordinates.Elevation.HasValue ? x.Coordinates.Elevation.Value : 0);
            var distance = geoCoordinate.GetDistanceTo(destCoord);

            distances.Add(
                new StationDistance
                {
                    Id = x.Id,
                    Distance = distance,
                });
        });

        return distances;
    }
}
