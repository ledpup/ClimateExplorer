namespace ClimateExplorer.Core.Model;

using System.Text.RegularExpressions;

/// <summary>
/// Abbreviations in station names, and what they stand for. Initials ("AWS", "AP") are kept in capitals by
/// <see cref="Station.ToDisplayName"/>; shortened words ("Intl", "Rgnl") stay title case. Both are matched ignoring
/// case, so names formatted before the initials list existed ("Bellambi Aws") are still found.
/// </summary>
public static partial class StationAbbreviations
{
    private static readonly Dictionary<string, (string Abbreviation, string Meaning)> Initialisms = ToLookup(
    [
        ("AFB", "Air Force Base"),
        ("AMO", "Airport Meteorological Office"),
        ("AP", "Airport"),
        ("AWS", "Automatic weather station"),
        ("CDA", "Canada Department of Agriculture"),
        ("CS", "Climate station"),
        ("DC", "District of Columbia"),
        ("FAA", "Federal Aviation Administration"),
        ("FAP", "Fuerza Aérea del Perú (Peruvian Air Force)"),
        ("HQ", "Headquarters"),
        ("JFK", "John F. Kennedy"),
        ("MCAF", "Marine Corps Air Facility"),
        ("MCAS", "Marine Corps Air Station"),
        ("NAF", "Naval Air Facility"),
        ("NAS", "Naval Air Station"),
        ("NPS", "National Park Service"),
        ("NWR", "National Wildlife Refuge"),
        ("NWS", "National Weather Service"),
        ("RAAF", "Royal Australian Air Force"),
        ("RCS", "Reference climate station"),
        ("SMHI", "Swedish Meteorological and Hydrological Institute"),
        ("UNC", "University of Northern Colorado"),
        ("WFO", "Weather forecast office"),
        ("WSO", "Weather service office"),
        ("WTP", "Water treatment plant"),
        ("WWTP", "Wastewater treatment plant"),
    ]);

    // Kept in capitals, but not worth explaining.
    private static readonly HashSet<string> RomanNumerals = new(StringComparer.OrdinalIgnoreCase) { "II", "III", "IV" };

    private static readonly Dictionary<string, (string Abbreviation, string Meaning)> ShortenedWords = ToLookup(
    [
        ("Aero", "Aerodrome"),
        ("Ag", "Agricultural"),
        ("Airp", "Airport"),
        ("Arpt", "Airport"),
        ("Aut", "Automatic station"),
        ("Capt", "Captain"),
        ("Cntrl", "Central"),
        ("Co", "County"),
        ("Cptl", "Capital"),
        ("Ctr", "Center"),
        ("Cy", "City"),
        ("Dwtn", "Downtown"),
        ("Exp", "Experiment"),
        ("Fld", "Field"),
        ("Fm", "Farm"),
        ("Gen", "General"),
        ("Gtr", "Greater"),
        ("Int", "International"),
        ("Int'l", "International"),
        ("Intl", "International"),
        ("Isl", "Island"),
        ("Ld", "Lock and dam"),
        ("Mem", "Memorial"),
        ("Mtn", "Mountain"),
        ("Muni", "Municipal"),
        ("Np", "National park"),
        ("Pk", "Park"),
        ("Plt", "Plant"),
        ("Rgnl", "Regional"),
        ("Rs", "Ranger station"),
        ("Rsch", "Research"),
        ("Spgs", "Springs"),
        ("Sta", "Station"),
        ("Stn", "Station"),
        ("Twr", "Tower"),
        ("Univ", "University"),
        ("Wks", "Works"),
        ("Wpc", "Water pollution control"),
        ("Wtr", "Water"),
    ]);

    // Abbreviations of more than one word, matched before the words on their own: "Univ Wi" is one entry, not
    // "Univ" and an unexplained "Wi". Written with single spaces.
    private static readonly Dictionary<string, (string Abbreviation, string Meaning)> Phrases = ToLookup(
    [
        ("Univ Wi", "University of Wisconsin"),
    ]);

    private static readonly int LongestPhraseWords = Phrases.Keys.Max(x => x.Split(' ').Length);

    /// <summary>
    /// Whether <see cref="Station.ToDisplayName"/> keeps a word in capitals.
    /// </summary>
    public static bool IsInitialism(string word)
    {
        return Initialisms.ContainsKey(word) || RomanNumerals.Contains(word);
    }

    /// <summary>
    /// The abbreviations in a name and what they stand for, each once, in the order they first appear. An
    /// abbreviation is given in its usual spelling ("AWS", "Intl") whatever its case in the name.
    /// </summary>
    public static List<(string Abbreviation, string Meaning)> Find(string name)
    {
        List<(string Abbreviation, string Meaning)> found = [];
        var words = WordRegex().Matches(name).Select(x => x.Value).ToList();
        var i = 0;
        while (i < words.Count)
        {
            var (entry, wordCount) = FindAt(words, i);
            if (entry is { } value && !found.Contains(value))
            {
                found.Add(value);
            }

            i += wordCount;
        }

        return found;
    }

    // A word in a station name: letters and digits, with apostrophes kept in ("O'Neill", "Int'l").
    [GeneratedRegex(@"[\p{L}\p{N}']+")]
    internal static partial Regex WordRegex();

    // The longest abbreviation starting at a word, and how many words it takes; one word when there's none.
    private static ((string Abbreviation, string Meaning)? Entry, int WordCount) FindAt(List<string> words, int start)
    {
        for (var count = Math.Min(LongestPhraseWords, words.Count - start); count > 1; count--)
        {
            if (Phrases.TryGetValue(string.Join(' ', words.Skip(start).Take(count)), out var phrase))
            {
                return (phrase, count);
            }
        }

        var word = words[start];
        return Initialisms.TryGetValue(word, out var entry) || ShortenedWords.TryGetValue(word, out entry)
            ? (entry, 1)
            : (null, 1);
    }

    private static Dictionary<string, (string Abbreviation, string Meaning)> ToLookup(List<(string Abbreviation, string Meaning)> entries)
    {
        return entries.ToDictionary(x => x.Abbreviation, StringComparer.OrdinalIgnoreCase);
    }
}
