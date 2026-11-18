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
        foreach (Match match in WordRegex().Matches(name))
        {
            if ((Initialisms.TryGetValue(match.Value, out var entry) || ShortenedWords.TryGetValue(match.Value, out entry))
                && !found.Contains(entry))
            {
                found.Add(entry);
            }
        }

        return found;
    }

    // A word in a station name: letters and digits, with apostrophes kept in ("O'Neill", "Int'l").
    [GeneratedRegex(@"[\p{L}\p{N}']+")]
    internal static partial Regex WordRegex();

    private static Dictionary<string, (string Abbreviation, string Meaning)> ToLookup(List<(string Abbreviation, string Meaning)> entries)
    {
        return entries.ToDictionary(x => x.Abbreviation, StringComparer.OrdinalIgnoreCase);
    }
}
