namespace ClimateExplorer.Data.Ecad;

using ClimateExplorer.Core;

/// <summary>
/// A station as published by the ECA&amp;D non-blended collection's <c>/locations</c> listing. Its
/// <see cref="Id"/> is ECA&amp;D's own <c>ecad_{staid:0000000}</c> identifier; the collection exposes no
/// WMO id, so it cannot be joined to a GHCN station by identifier alone (see <see cref="EcadStationMatcher"/>).
/// </summary>
public sealed record EcadStation(
    string Id,
    string? Name,
    string? CountryCode,
    Coordinates Coordinates,
    IReadOnlyList<EcadStationSeries> Series)
{
    /// <summary>
    /// The series in a measurement family (see <see cref="EcadConstants.IsInFamily"/>) whose observations
    /// run at least as recently as <paramref name="observedOnOrAfter"/>. A station that stopped reporting
    /// a family years ago is of no use here: ECA&amp;D is declared ahead of GHCNd, so mapping a station
    /// against a dead series would replace a live GHCNd series with a stale one.
    /// </summary>
    public IReadOnlyList<EcadStationSeries> GetCurrentSeries(string familyPrefix, DateOnly observedOnOrAfter)
    {
        return [.. Series
            .Where(x => EcadConstants.IsInFamily(x.ParameterCode, familyPrefix) && x.LastDate >= observedOnOrAfter)
            .OrderBy(x => x.ParameterCode, StringComparer.Ordinal)];
    }
}

/// <summary>
/// One parameter code a station reports, and the span it reports it over. Where two data providers
/// contribute the same code for the same station, their spans are merged into a single outer range.
/// </summary>
public sealed record EcadStationSeries(string ParameterCode, DateOnly FirstDate, DateOnly LastDate);
