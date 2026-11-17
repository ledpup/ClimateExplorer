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
    public const int TrendScoreMaximumYearsBehindNewest = 10;

    /// <summary>The trend score of a location whose since-1961 warming rate is negative.</summary>
    public const short NegativeRateTrendScore = -1;

    /// <summary>The percentile of non-negative since-1961 rates at which the trend score steps start: lower rates score 0.</summary>
    public const double TrendScoreLowPercentile = 0.01;

    /// <summary>The percentile of non-negative since-1961 rates at which the trend score steps end: higher rates score 9.</summary>
    public const double TrendScoreHighPercentile = 0.99;

    private const short TitleMaximumLength = 30;
    public required string CountryCode { get; set; }
    public string? Country { get; set; }
    public required Coordinates Coordinates { get; set; }
    public double? WarmingAnomaly { get; set; }
    public short? TrendScore { get; set; }

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
    /// Scores each location's since-1961 warming rate (<see cref="Since1961TemperatureTrend"/>) on ten equal-width
    /// steps, from 9 (fastest) to 0. The steps span the rates of zero or more between the
    /// <see cref="TrendScoreLowPercentile"/> and <see cref="TrendScoreHighPercentile"/> of those rates, so a few
    /// outliers don't stretch the scale: rates above the range score 9 and rates below it score 0. A negative
    /// rate scores <see cref="NegativeRateTrendScore"/>. Only locations with a since-1961 trend and a last year
    /// within <see cref="TrendScoreMaximumYearsBehindNewest"/> of the newest are scored; the rest get no score.
    /// </summary>
    /// <returns>The range of rates each score covers, in °C per decade.</returns>
    public static List<TrendScoreRow> SetTrendScores(IEnumerable<Location> locations)
    {
        var allLocations = locations.Where(x => x is not null).ToList();
        allLocations.ForEach(x => x.TrendScore = null);

        var eligible = GetEligibleForTrendScore(allLocations);
        var scale = TrendScoreScale.Create(eligible);

        foreach (var location in eligible)
        {
            var slope = location.Since1961TemperatureTrend!.Slope;
            location.TrendScore = slope < 0 ? NegativeRateTrendScore : scale!.Score(slope);
        }

        return scale?.GetTable() ?? [];
    }

    /// <summary>
    /// The trend score table for these locations, without setting their scores: the rows
    /// <see cref="SetTrendScores"/> returns, then a row for <see cref="NegativeRateTrendScore"/> (the range of
    /// the negative rates and the number of locations), then a last row, with a null score, counting the locations
    /// that have no score.
    /// </summary>
    public static List<TrendScoreRow> GetTrendScoreTable(IEnumerable<Location> locations)
    {
        var allLocations = locations.Where(x => x is not null).ToList();
        var eligible = GetEligibleForTrendScore(allLocations);
        var table = TrendScoreScale.Create(eligible)?.GetTable() ?? [];

        var negativeRates = eligible
            .Select(x => x.Since1961TemperatureTrend!.Slope)
            .Where(x => x < 0)
            .ToList();
        table.Add(
            new TrendScoreRow
            {
                Score = NegativeRateTrendScore,
                MinimumRatePerDecade = negativeRates.Count == 0 ? null : negativeRates.Min() * 10,
                MaximumRatePerDecade = negativeRates.Count == 0 ? null : negativeRates.Max() * 10,
                LocationCount = negativeRates.Count,
            });

        table.Add(new TrendScoreRow { Score = null, MinimumRatePerDecade = null, MaximumRatePerDecade = null, LocationCount = allLocations.Count - eligible.Count });

        return table;
    }

    public override string ToString()
    {
        return FullTitle;
    }

    public string UrlReadyName()
    {
        return Name.ToLower().Replace(" ", "-").Replace("'", "-");
    }

    private static List<Location> GetEligibleForTrendScore(List<Location> allLocations)
    {
        var newestLastYear = allLocations.Max(x => x.LastYear);
        return allLocations
            .Where(x => x.Since1961TemperatureTrend is not null && x.LastYear >= newestLastYear - TrendScoreMaximumYearsBehindNewest)
            .ToList();
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

/// <summary>The ten equal-width steps of the trend score, over the since-1961 rates (per year) of the scored locations.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Not important")]
internal sealed class TrendScoreScale
{
    private const int StepCount = 10;
    private const double PerDecade = 10;

    private readonly double low;
    private readonly double step;
    private readonly List<double> sortedRates;

    private TrendScoreScale(List<double> sortedRates)
    {
        this.sortedRates = sortedRates;
        low = Percentile(sortedRates, Location.TrendScoreLowPercentile);
        step = (Percentile(sortedRates, Location.TrendScoreHighPercentile) - low) / StepCount;
    }

    /// <summary>The scale for the eligible locations, or null when none has a rate of zero or more.</summary>
    public static TrendScoreScale? Create(IEnumerable<Location> eligible)
    {
        var rates = eligible
            .Select(x => x.Since1961TemperatureTrend!.Slope)
            .Where(x => x >= 0)
            .Order()
            .ToList();

        if (rates.Count == 0)
        {
            return null;
        }

        return new TrendScoreScale(rates);
    }

    public short Score(double rate)
    {
        // No spread (one location, or every rate the same): nothing to tell the locations apart by.
        if (step <= 0)
        {
            return 0;
        }

        return (short)Math.Clamp(Math.Floor((rate - low) / step), 0, StepCount - 1);
    }

    public List<TrendScoreRow> GetTable()
    {
        var minimumRate = sortedRates[0];
        var maximumRate = sortedRates[^1];

        if (step <= 0)
        {
            return [new TrendScoreRow { MinimumRatePerDecade = minimumRate * PerDecade, MaximumRatePerDecade = maximumRate * PerDecade, Score = 0, LocationCount = sortedRates.Count }];
        }

        var counts = sortedRates.GroupBy(x => Score(x)).ToDictionary(x => (int)x.Key, x => x.Count());

        // The top and bottom scores also hold the outliers, so their ranges run out to the actual extremes.
        return Enumerable.Range(0, StepCount)
            .Reverse()
            .Select(score => new TrendScoreRow
            {
                MinimumRatePerDecade = (score == 0 ? Math.Min(minimumRate, low) : low + (score * step)) * PerDecade,
                MaximumRatePerDecade = (score == StepCount - 1 ? Math.Max(maximumRate, low + (StepCount * step)) : low + ((score + 1) * step)) * PerDecade,
                Score = score,
                LocationCount = counts.GetValueOrDefault(score),
            })
            .ToList();
    }

    // Nearest rank, rounding down: the rate at this fraction of the way through the sorted rates.
    private static double Percentile(List<double> sortedRates, double fraction) =>
        sortedRates[(int)(fraction * (sortedRates.Count - 1))];
}
