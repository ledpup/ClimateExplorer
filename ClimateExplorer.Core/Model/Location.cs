namespace ClimateExplorer.Core.Model;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;

public class Location : GeographicalEntity
{
    /// <summary>
    /// A location is only scored when its last year is within this many years of the newest last year
    /// of any location, so a station that closed decades ago isn't ranked on a shorter window.
    /// </summary>
    public const int HeatingScoreMaximumYearsBehindNewest = 10;

    /// <summary>The heating score of a location whose since-1961 warming rate is negative.</summary>
    public const short NegativeRateHeatingScore = -1;

    private const short TitleMaximumLength = 30;
    public required string CountryCode { get; set; }
    public string? Country { get; set; }
    public required Coordinates Coordinates { get; set; }
    public double? WarmingAnomaly { get; set; }
    public short? HeatingScore { get; set; }

    /// <summary>
    /// Linear trends of the location's yearly temperature series. Null below 30 complete years; the
    /// full-record trend only for 30-59 years; full record then last 30 years from 60. A since-1961 trend
    /// follows whenever its window is eligible (at least 30 complete years from 1961, the first no later
    /// than 1965; see <see cref="Stats.TrendWindows.GetSince1961Points"/>).
    /// </summary>
    public List<TrendSummary>? TemperatureTrends { get; set; }

    public int? LastYear { get; set; }

    public DataRecord? RecordHigh { get; set; }

    [JsonIgnore]
    public TrendSummary? FullPeriodTemperatureTrend => TemperatureTrends?.FirstOrDefault(x => x.Window == TrendWindow.Full);

    [JsonIgnore]
    public TrendSummary? Last30YearTemperatureTrend => TemperatureTrends?.FirstOrDefault(x => x.Window == TrendWindow.Last30);

    [JsonIgnore]
    public TrendSummary? Since1961TemperatureTrend => TemperatureTrends?.FirstOrDefault(x => x.Window == TrendWindow.Since1961);

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

    [JsonIgnore]
    public string CountryWithoutOwner
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

    /// <summary>
    /// Scores each location's since-1961 warming rate (<see cref="Since1961TemperatureTrend"/>) against
    /// every other location's. The rates of zero or more are ranked into tenths, from 9 (the fastest 10%)
    /// to 0, and a negative rate scores <see cref="NegativeRateHeatingScore"/>. Only locations with a
    /// since-1961 trend and a last year within <see cref="HeatingScoreMaximumYearsBehindNewest"/> of the
    /// newest are scored; the rest get no score.
    /// </summary>
    /// <returns>The range of rates each score covers, in °C per decade.</returns>
    public static List<HeatingScoreRow> SetHeatingScores(IEnumerable<Location> locations)
    {
        var allLocations = locations.Where(x => x is not null).ToList();
        allLocations.ForEach(x => x.HeatingScore = null);

        var newestLastYear = allLocations.Max(x => x.LastYear);
        var eligible = allLocations
            .Where(x => x.Since1961TemperatureTrend is not null && x.LastYear >= newestLastYear - HeatingScoreMaximumYearsBehindNewest)
            .ToList();

        var ranked = eligible
            .Where(x => x.Since1961TemperatureTrend!.Slope >= 0)
            .OrderByDescending(x => x.Since1961TemperatureTrend!.Slope)
            .ToList();
        var tenPercent = (int)MathF.Round(ranked.Count * .1f, 0);
        var heatingScoreTable = new List<HeatingScoreRow>();
        var scoredCount = 0;

        for (short score = 9; score >= 0; score--)
        {
            // Score 0 takes whatever is left, in case rounding leaves slightly more than 10%.
            var band = ranked.Skip(scoredCount).Take(score == 0 ? ranked.Count : tenPercent).ToList();
            scoredCount += band.Count;

            if (band.Count == 0)
            {
                continue;
            }

            heatingScoreTable.Add(
                new HeatingScoreRow
                {
                    MaximumRatePerDecade = band[0].Since1961TemperatureTrend!.Slope * 10,
                    MinimumRatePerDecade = band[^1].Since1961TemperatureTrend!.Slope * 10,
                    Score = score,
                });

            band.ForEach(x => x.HeatingScore = score);
        }

        eligible
            .Where(x => x.Since1961TemperatureTrend!.Slope < 0)
            .ToList()
            .ForEach(x => x.HeatingScore = NegativeRateHeatingScore);

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