using GeoCoordinatePortable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ClimateExplorer.Core.Model;
public class Location : LocationBase
{
    public required string? CountryCode { get; set; }
    public string? Country { get; set; }
    public required Coordinates Coordinates { get; set; }
    public float? WarmingIndex { get; set; }
    public short? HeatingScore { get; set; }
    public List<LocationDistance>? NearbyLocations { get; set; }

    [JsonIgnore]
    public string FullTitle
    {
        get
        {
            if (fullTitle != null)
                return fullTitle;

            fullTitle = Country == null ? Name : $"{Name}, {Country}";

            return fullTitle;
        }
    }
    string? fullTitle;

    [JsonIgnore]
    public string ShorterTitle
    {
        get
        {
            if (shorterTitle != null)
                return shorterTitle;

            shorterTitle = $"{Name}, {Country}";

            var regex = new Regex(@"(?<country>.*)\s(?<owner>\[.*\])");
            var match = regex.Match(Country!);
            if (match.Success)
            {
                var country = match.Groups["country"];
                shorterTitle = $"{Name}, {country}";
            }

            return shorterTitle;
        }
    }
    string? shorterTitle;

    public const int TitleMaximumLength = 20;

    [JsonIgnore]
    public string Title
    {
        get
        {
            if (title != null)
                return title;

            if (FullTitle.Length <= TitleMaximumLength)
            {
                title = FullTitle;
            }
            else if (ShorterTitle.Length <= TitleMaximumLength)
            {
                title = ShorterTitle;
            }
            else if (Name.Length < 8)
            {
                elipsisApplied = true;
                title = ShorterTitle.Truncate(TitleMaximumLength - 3);
            }
            else if (Name.Length > TitleMaximumLength)
            {
                elipsisApplied = true;
                title = Name.Truncate(TitleMaximumLength - 3);
            }
            else
            {
                title = Name;
            }
            
            return title;
        }
    }
    string? title;
    bool elipsisApplied;

    public bool IsLongTitle
    {
        get 
        {
            return elipsisApplied || Title.Length > TitleMaximumLength || FullTitle.Length > TitleMaximumLength;
        }
    }

    public static async Task<List<Location>> GetLocationsFromFile(string pathAndFileName)
    {
        var locationText = await File.ReadAllTextAsync(pathAndFileName);
        var locations = JsonSerializer.Deserialize<List<Location>>(locationText);
        await SetCountries(locations!);
        return locations!;
    }

    public static async Task<List<Location>> GetLocations(string? folderName = null)
    {
        folderName = folderName ?? @"MetaData\Location";
        var locations = new List<Location>();
        var locationFiles = Directory.GetFiles(folderName).ToList();
        foreach (var file in locationFiles)
        {
            var locationsInFile = await GetLocationsFromFile(file);
            locations.AddRange(locationsInFile);
        }

        locations.Add(new Location { Id = new Guid("143983a0-240e-447f-8578-8daf2c0a246a"), Name = "Australia anomaly", CountryCode = "AS", Coordinates = new Coordinates() });

        locations = locations.OrderBy(x => x.Name).ToList();

        return locations;
    }

    private static async Task SetCountries(List<Location> locations)
    {
        var countries = await Model.Country.GetCountries(@"MetaData\countries.txt");
        locations.ForEach(x => x.Country = countries[x.CountryCode!].Name);
    }

    public static void SetNearbyLocations(List<Location>? locations)
    {
        Parallel.ForEach(locations!, location =>
        {
            var distances = GetDistances(location, locations!);

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
            var distance = Math.Round(geoCoordinate.GetDistanceTo(destCoord) / 1000, 0); // GetDistanceTo is in metres. Convert to km
            var bearing = Math.Round(GeometryHelpers.GetRhumbBearingFromPointToPoint(geoCoordinate, destCoord), 0);

            distances.Add(
                new LocationDistance
                {
                    LocationId = x.Id,
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
    public required Guid LocationId { get; set; }
    public required double Distance { get; set; }
    public required double BearingDegrees { get; set; }
    public required string CompassRoseDirection { get; set; }
}