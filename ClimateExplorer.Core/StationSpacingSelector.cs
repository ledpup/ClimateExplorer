namespace ClimateExplorer.Core;

using GeoCoordinatePortable;

/// <summary>
/// Picks stations so that no two in the same group (country) sit closer than a radius, keeping the better
/// station of any close pair. Candidates are taken in priority order: each is kept unless a station already
/// kept in its group, or a seed, is within the candidate's own radius. A station is only ever blocked by an
/// actual neighbour, so clusters can't chain across a region the way density clustering does.
/// </summary>
public static class StationSpacingSelector
{
    /// <param name="seeds">Locations that already exist (from another source). They block candidates near them but aren't returned.</param>
    /// <param name="candidatesInPriorityOrder">Candidates, best first.</param>
    /// <param name="group">Stations are only compared with others in the same group.</param>
    /// <param name="coordinates">Where the station is.</param>
    /// <param name="radiusKm">How close a kept station must be to block this candidate.</param>
    /// <returns>The kept candidates, in priority order.</returns>
    public static List<T> Select<T>(
        IEnumerable<T> seeds,
        IEnumerable<T> candidatesInPriorityOrder,
        Func<T, string> group,
        Func<T, Coordinates> coordinates,
        Func<T, double> radiusKm)
    {
        var keptByGroup = new Dictionary<string, List<GeoCoordinate>>();
        foreach (var seed in seeds)
        {
            GetGroup(keptByGroup, group(seed)).Add(ToGeoCoordinate(coordinates(seed)));
        }

        var selected = new List<T>();
        foreach (var candidate in candidatesInPriorityOrder)
        {
            var kept = GetGroup(keptByGroup, group(candidate));
            var position = ToGeoCoordinate(coordinates(candidate));
            var radiusMetres = radiusKm(candidate) * 1000;

            if (kept.Any(x => x.GetDistanceTo(position) < radiusMetres))
            {
                continue;
            }

            kept.Add(position);
            selected.Add(candidate);
        }

        return selected;
    }

    private static List<GeoCoordinate> GetGroup(Dictionary<string, List<GeoCoordinate>> keptByGroup, string group)
    {
        if (!keptByGroup.TryGetValue(group, out var kept))
        {
            kept = [];
            keptByGroup[group] = kept;
        }

        return kept;
    }

    private static GeoCoordinate ToGeoCoordinate(Coordinates coordinates)
    {
        return new GeoCoordinate(coordinates.Latitude, coordinates.Longitude);
    }
}
