namespace ClimateExplorer.Core.Model;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public partial class Station
{
    public required string Id { get; set; }
    public string? Name { get; set; }
    public string? CountryCode { get; set; }
    public Coordinates? Coordinates { get; set; }
    public int? FirstYear { get; set; }
    public int? LastYear { get; set; }
    public int? YearsOfMissingData { get; set; }

    [JsonIgnore]
    public int? Age => LastYear - FirstYear + 1;

    [JsonIgnore]
    public List<StationDistance>? StationDistances { get; set; }

    [JsonIgnore]
    public double? AverageDistance { get; set; }

    [JsonIgnore]
    public int? Score
    {
        get
        {
            return Age - YearsOfMissingData;
        }
    }

    [JsonIgnore]
    public string? Source { get; set; }

    public static async Task<List<Station>> GetStationsFromFiles(List<string> pathAndFileNames)
    {
        var stations = new List<Station>();
        foreach (var pathAndFileName in pathAndFileNames)
        {
            var list = await GetStationsFromFile(pathAndFileName);
            stations.AddRange(list!);
        }

        return stations;
    }

    public static async Task<List<Station>> GetStationsFromFile(string pathAndFileName)
    {
        var text = await File.ReadAllTextAsync(pathAndFileName);
        var list = JsonSerializer.Deserialize<List<Station>>(text)!;
        return list;
    }

    /// <summary>
    /// A station name as people read it. Sources such as GHCN and BOM give names in upper case with underscores
    /// ("AVALON_AIRPORT"); those become title case with spaces ("Avalon Airport"). A name with any lower-case letter
    /// is taken as already formatted, and only loses its underscores.
    /// </summary>
    /// <remarks>
    /// Title casing keeps the initials in <see cref="StationAbbreviations"/> in capitals ("Bellambi AWS", "Atlanta
    /// Peachtree AP"), as well as compass
    /// offsets ("Dallas 7 NE", "Troy 3N"). It also capitalises after "Mc" ("McKinley Park") and after a one-letter
    /// prefix and apostrophe ("O'Neill", "M'Sila").
    /// </remarks>
    public static string ToDisplayName(string name)
    {
        var spaced = name.Replace('_', ' ');
        if (spaced.Any(char.IsLower))
        {
            return spaced;
        }

        var titleCase = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.ToLowerInvariant());
        return StationAbbreviations.WordRegex().Replace(titleCase, x => ToDisplayWord(x.Value));
    }

    public override string ToString()
    {
        return Id;
    }

    private static string ToDisplayWord(string word)
    {
        var upper = word.ToUpperInvariant();
        if (StationAbbreviations.IsInitialism(upper) || CompassOffsetRegex().IsMatch(upper))
        {
            return upper;
        }

        // "Mckinley" -> "McKinley"; "O'neill" -> "O'Neill"
        if (word.Length > 2 && (word.StartsWith("Mc", StringComparison.Ordinal) || word[1] == '\'') && char.IsLetter(word[2]))
        {
            return string.Concat(word.AsSpan(0, 2), char.ToUpperInvariant(word[2]).ToString(), word.AsSpan(3));
        }

        return word;
    }

    // A compass point, optionally after a distance: "NE", "SSW", "3N", "4WNW"
    [GeneratedRegex(@"^\d*(N|S|E|W|NE|NW|SE|SW|NNE|ENE|ESE|SSE|SSW|WSW|WNW|NNW)$")]
    private static partial Regex CompassOffsetRegex();
}
