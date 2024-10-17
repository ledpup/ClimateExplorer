namespace ClimateExplorer.Core.Model;

using GeoCoordinatePortable;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public class Location : GeographicalEntity
{
    public const int TitleMaximumLength = 18;

    private string? title;
    private string? fullTitle;
    private string? shorterTitle;

    required public string CountryCode { get; set; }
    public string? Country { get; set; }
    required public Coordinates Coordinates { get; set; }
    public double? WarmingAnomaly { get; set; }
    public short? HeatingScore { get; set; }
    public List<LocationDistance>? NearbyLocations { get; set; }

    [JsonIgnore]
    public string FullTitle
    {
        get
        {
            if (fullTitle != null)
            {
                return fullTitle;
            }

            fullTitle = Country == null ? Name : $"{Name}, {Country}";

            return fullTitle;
        }
    }

    [JsonIgnore]
    public string ShorterTitle
    {
        get
        {
            if (shorterTitle != null)
            {
                return shorterTitle;
            }

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

    [JsonIgnore]
    public string Title
    {
        get
        {
            if (title != null)
            {
                return title;
            }

            if (FullTitle.Length <= TitleMaximumLength)
            {
                title = FullTitle;
            }
            else if (ShorterTitle.Length <= TitleMaximumLength)
            {
                title = ShorterTitle;
            }
            else
            {
                title = Name;
            }

            return title;
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
        folderName ??= @"MetaData\Location";
        var locations = new List<Location>();
        var locationFiles = Directory.GetFiles(folderName).ToList();
        foreach (var file in locationFiles)
        {
            var locationsInFile = await GetLocationsFromFile(file);
            locations.AddRange(locationsInFile);
        }

        locations = [.. locations.OrderBy(x => x.Name)];

        return locations;
    }

    public static Dictionary<Guid, List<LocationDistance>> GenerateNearbyLocations(IEnumerable<Location>? locations)
    {
        var nearbyLocations = new ConcurrentDictionary<Guid, List<LocationDistance>>();
        Parallel.ForEach(locations!, location =>
        {
            var distances = GetDistances(location, locations!);

            var nearby = distances.OrderBy(x => x.Distance).Take(10).ToList();

            nearbyLocations.TryAdd(location.Id, nearby);
        });
        return nearbyLocations.ToDictionary();
    }

    public static List<LocationDistance> GetDistances(Location location, IEnumerable<Location> locations)
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
                    CompassRoseDirection = GeometryHelpers.GetCompassRoseDirectionName(bearing),
                });
        });

        return distances;
    }

    public static List<HeatingScoreRow> SetHeatingScores(IEnumerable<Location> locations)
    {
        var locationsWithWarming = locations.Where(x => x?.WarmingAnomaly >= 0);
        var numberOfLocations = locationsWithWarming.Count();
        var locationsOrderedByWarming = locationsWithWarming.OrderByDescending(x => x.WarmingAnomaly);
        var tenPercent = (int)MathF.Round(numberOfLocations * .1f, 0);
        var heatingScoreTable = new List<HeatingScoreRow>();
        for (short i = 9; i >= 0; i--)
        {
            var nextTenPercent = locationsOrderedByWarming
                .Where(x => x.HeatingScore == null)
                .Take(tenPercent)
                .ToList();

            // In case there is a rounding error and slightly more than 10% left over, assign all the remaining to a score of zero
            if (i == 0)
            {
                nextTenPercent = locationsOrderedByWarming
                    .Where(x => x.HeatingScore == null)
                    .ToList();
            }

            if (nextTenPercent.Any())
            {
                heatingScoreTable.Add(
                    new HeatingScoreRow
                    {
                        MaximumWarmingAnomaly = nextTenPercent.First().WarmingAnomaly!.Value,
                        MinimumWarmingAnomaly = nextTenPercent.Last().WarmingAnomaly!.Value,
                        Score = i,
                    });
            }

            nextTenPercent.ForEach(x => x.HeatingScore = i);
        }

        // If the WarmingAnomaly is negative, use that as the HeatingScore, rounded to 0 decimal places
        locations
            .Where(x => x?.WarmingAnomaly < 0)
            .ToList()
            .ForEach(x => x.HeatingScore = (short)Math.Round(x.WarmingAnomaly!.Value, 0));

        return heatingScoreTable;
    }

    public override string ToString()
    {
        return FullTitle;
    }

    public string UrlReadyName()
    {
        return Name.ToLower().Replace(" ", "-").Replace("'", "-");
    }

    private static async Task SetCountries(List<Location> locations)
    {
        var countries = await Model.Country.GetCountries(@"MetaData\countries.txt");
        locations.ForEach(x => x.Country = countries[x.CountryCode!].Name);
    }
}

public class LocationDistance
{
    required public Guid LocationId { get; set; }
    required public double Distance { get; set; }
    required public double BearingDegrees { get; set; }
    required public string CompassRoseDirection { get; set; }
}