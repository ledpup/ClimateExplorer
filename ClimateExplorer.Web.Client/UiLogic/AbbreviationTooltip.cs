namespace ClimateExplorer.Web.UiLogic;

using System.Net;
using ClimateExplorer.Core.Model;

/// <summary>
/// The part of a location name's hover tooltip that explains the abbreviations in the name ("AWS: Automatic weather
/// station"). The meanings are bundled in <see cref="StationAbbreviations"/>, so there's nothing to fetch.
/// </summary>
public static class AbbreviationTooltip
{
    /// <summary>
    /// One line per abbreviation, or null when the name has none.
    /// </summary>
    public static string? Build(string name)
    {
        var abbreviations = StationAbbreviations.Find(name);
        if (abbreviations.Count == 0)
        {
            return null;
        }

        return "<p>" + string.Join("<br>", abbreviations.Select(x => $"<b>{WebUtility.HtmlEncode(x.Abbreviation)}</b>: {WebUtility.HtmlEncode(x.Meaning)}")) + "</p>";
    }
}
