namespace ClimateExplorer.Data.Ghcnm;

using System.Text.RegularExpressions;

internal static partial class GhcnmStationIds
{
    /// <summary>
    /// NOAA renumbered Canadian stations in both GHCNm and GHCNd (seen in the 2026-11 files) from
    /// CA00nnnnnnn to CAN0nnnnnnn, e.g. Banff CA003050520 became CAN03050520. Returns the ID the station had
    /// before, so it keeps its location ID; null when the ID wasn't renumbered.
    /// </summary>
    public static string? GetPreviousId(string id)
    {
        var match = RenumberedCanadianId().Match(id);
        return match.Success ? "CA00" + match.Groups["number"].Value : null;
    }

    [GeneratedRegex(@"^CAN0(?<number>\d{7})$")]
    private static partial Regex RenumberedCanadianId();
}
