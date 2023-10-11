using GeoCoordinatePortable;

namespace ClimateExplorer.Core.Model;
public class StationDistance
{
    public required string? Id { get; set; }
    public double Distance { get; set; }

    public static List<StationDistance>? GetDistances(Station station, List<Station> stations)
    {
        if (station.Coordinates == null)
        {
            throw new Exception($"Unable to get distances for station {station.Id} as it has no coordinates");
        }
        var coords = station.Coordinates.Value;
        var originCoord = new GeoCoordinate(coords.Latitude, coords.Longitude, coords.Elevation ?? 0);
        return GetDistances(originCoord, stations.Where(x => x != station));
    }

    public static List<StationDistance> GetDistances(GeoCoordinate geoCoordinate, IEnumerable<Station> stations)
    {
        var distances = new List<StationDistance>();

        stations.ToList().ForEach(x =>
        {
            if (x.Coordinates == null)
            {
                throw new Exception($"Unable to get distances for station {x.Id} as it has no coordinates");
            }
            var coords = x.Coordinates.Value;
            var destCoord = new GeoCoordinate(coords.Latitude, coords.Longitude, coords.Elevation ?? 0);
            var distance = geoCoordinate.GetDistanceTo(destCoord) / 1000; // GetDistanceTo is in metres. Convert to km

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
