namespace ClimateExplorer.Core.Model;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;

public class Location : GeographicalEntity
{
    private const short TitleMaximumLength = 30;
    public required string CountryCode { get; set; }
    public string? Country { get; set; }
    public required Coordinates Coordinates { get; set; }
    public double? WarmingAnomaly { get; set; }
    public short? HeatingScore { get; set; }
    public DataRecord? RecordHigh { get; set; }

    [JsonIgnore]
    public string FullTitle =>
        string.IsNullOrWhiteSpace(Country)
            ? Name
            : $"{Name}, {Country}";

    [JsonIgnore]
    public string CodeTitle =>
        string.IsNullOrWhiteSpace(CountryCode)
            ? Name
            : $"{Name}, {CountryCode}";

    [JsonIgnore]
    public string DisplayTitle
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CountryWithoutOwner))
            {
                return Name;
            }

            var proposedTitle = $"{Name}, {CountryWithoutOwner}";

            var country = proposedTitle?.Length > TitleMaximumLength ? CountryCode : CountryWithoutOwner;

            return $"{Name}, {country}";
        }
    }

    private string CountryWithoutOwner
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Country))
            {
                return string.Empty;
            }

            var match = Regex.Match(Country, @"^(?<country>.*?)\s*\[.*\]$");
            return match.Success
                ? match.Groups["country"].Value.Trim()
                : Country;
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

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Not important")]
public class LocationDistance
{
    public required Guid LocationId { get; set; }
    public required double Distance { get; set; }
    public required double BearingDegrees { get; set; }
    public required string CompassRoseDirection { get; set; }
}