namespace ClimateExplorer.Data.Ecad;

using ClimateExplorer.Core.Model;
using GeoCoordinatePortable;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Reconciles GHCN stations against ECA&amp;D non-blended stations.
/// <para>
/// The non-blended collection publishes no WMO id, so there is no deterministic join key: a station has
/// to be recognised by where it is and what it is called. That makes this a reconciliation step, not a
/// guaranteed 1:1 join, so anything it cannot resolve confidently is reported as a rejection with a
/// reason rather than resolved by guessing. Rejections are expected output, not errors - a maintainer
/// reads them to decide whether a manual override is worth adding.
/// </para>
/// </summary>
public static class EcadStationMatcher
{
    public static EcadStationMatchReport Match(
        IEnumerable<Station> ghcnStations,
        IEnumerable<EcadStation> ecadStations,
        EcadStationMatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(ghcnStations);
        ArgumentNullException.ThrowIfNull(ecadStations);
        ArgumentNullException.ThrowIfNull(options);

        // Only a station that can serve every published measurement is a candidate at all. Ranking the
        // whole neighbourhood and testing the winner afterwards would let a station that stopped reporting
        // years ago beat the live one next to it purely on its name, and lose the location entirely.
        var candidates = ecadStations
            .Select(x => new UsableStation(x, SelectSeries(x, options, out var missing, out var ambiguous), missing, ambiguous))
            .ToList();
        var usableCandidates = candidates.Where(x => x.IsUsable).ToList();

        var matches = new List<EcadStationMatch>();
        var rejections = new List<EcadStationRejection>();

        foreach (var ghcnStation in ghcnStations)
        {
            if (ghcnStation.Coordinates is not { } ghcnCoordinates)
            {
                continue;
            }

            var origin = new GeoCoordinate(ghcnCoordinates.Latitude, ghcnCoordinates.Longitude);
            var nearby = RankCandidates(origin, ghcnStation.Name, usableCandidates, options);
            if (nearby.Count == 0)
            {
                // Worth reporting only where a station that would otherwise have matched was passed over
                // for not reporting everything; a location with nothing of ECA&D's nearby is the norm.
                var unusable = RankCandidates(origin, ghcnStation.Name, candidates.Where(x => !x.IsUsable), options)
                    .FirstOrDefault(x => x.NameSimilarity >= options.MinimumNameSimilarity);
                if (unusable != null)
                {
                    rejections.Add(new EcadStationRejection(
                        ghcnStation.Id,
                        unusable.Candidate.AmbiguousDataTypes.Count > 0
                            ? EcadStationRejectionReason.AmbiguousParameterVariant
                            : EcadStationRejectionReason.IncompleteMeasurements,
                        Describe(unusable.Candidate) + (unusable.Candidate.AmbiguousDataTypes.Count > 0
                            ? $" reports more than one current variant for {string.Join(", ", unusable.Candidate.AmbiguousDataTypes)}."
                            : $" has no current {string.Join(", ", unusable.Candidate.MissingDataTypes)} series.")));
                }

                continue;
            }

            var best = nearby[0];
            if (best.NameSimilarity < options.MinimumNameSimilarity)
            {
                rejections.Add(new EcadStationRejection(
                    ghcnStation.Id,
                    EcadStationRejectionReason.NameNotCorroborated,
                    Describe(nearby.Take(3))));
                continue;
            }

            // Coordinate proximity alone routinely turns up several real stations in the same town, so the
            // name is what decides between them. Rivals scoring exactly as well as the best candidate are
            // only separable when they are the same station registered twice (identical normalised names,
            // which happens where two participants contribute the same site); anything else is a genuine
            // ambiguity and is left for a human.
            var rivals = nearby.Skip(1).Where(x => x.NameSimilarity >= best.NameSimilarity).ToList();
            var matchKind = EcadStationMatchKind.Unique;
            if (rivals.Count > 0)
            {
                var duplicateRegistrations = rivals.All(x => EcadStationNameComparer.IsSameName(x.Candidate.Station.Name, best.Candidate.Station.Name));
                if (!duplicateRegistrations)
                {
                    rejections.Add(new EcadStationRejection(
                        ghcnStation.Id,
                        EcadStationRejectionReason.Ambiguous,
                        Describe(nearby.Take(3))));
                    continue;
                }

                matchKind = EcadStationMatchKind.DuplicateRegistration;
                best = rivals
                    .Append(best)
                    .OrderByDescending(x => GetLatestObservation(x.Candidate.Station))
                    .ThenBy(x => x.DistanceKm)
                    .ThenBy(x => x.Candidate.Station.Id, StringComparer.Ordinal)
                    .First();
            }

            matches.Add(new EcadStationMatch(
                ghcnStation.Id,
                best.Candidate.Station.Id,
                best.Candidate.Station.Name,
                Math.Round(best.DistanceKm, 3),
                Math.Round(best.NameSimilarity, 3),
                matchKind,
                best.Candidate.Series));
        }

        // The same ECA&D station falling out as the best match for two different GHCN stations would put
        // the same series behind two locations; neither claim is more credible than the other, so both go.
        var contested = matches
            .GroupBy(x => x.EcadStationId, StringComparer.Ordinal)
            .Where(x => x.Count() > 1)
            .SelectMany(x => x)
            .ToList();
        foreach (var match in contested)
        {
            rejections.Add(new EcadStationRejection(
                match.GhcnStationId,
                EcadStationRejectionReason.Contested,
                $"{match.EcadStationId} is also the best match for another GHCN station."));
        }

        return new EcadStationMatchReport(
            [.. matches.Except(contested).OrderBy(x => x.GhcnStationId, StringComparer.Ordinal)],
            [.. rejections.OrderBy(x => x.GhcnStationId, StringComparer.Ordinal)]);
    }

    private static List<RankedCandidate> RankCandidates(
        GeoCoordinate origin,
        string? ghcnStationName,
        IEnumerable<UsableStation> candidates,
        EcadStationMatchOptions options)
    {
        return [.. candidates
            .Select(x => new
            {
                Candidate = x,
                DistanceKm = origin.GetDistanceTo(new GeoCoordinate(x.Station.Coordinates.Latitude, x.Station.Coordinates.Longitude)) / 1000d,
            })
            .Where(x => x.DistanceKm <= options.MaximumDistanceKm)
            .Select(x => new RankedCandidate(
                x.Candidate,
                x.DistanceKm,
                EcadStationNameComparer.GetSimilarity(ghcnStationName, x.Candidate.Station.Name)))
            .OrderByDescending(x => x.NameSimilarity)
            .ThenBy(x => x.DistanceKm)];
    }

    /// <summary>
    /// Picks the one accumulation-period variant the station currently reports for each published
    /// measurement. Every measurement is required: they all share a single source file, and a file whose
    /// precipitation column is empty throughout fails download validation outright.
    /// </summary>
    private static IReadOnlyDictionary<DataType, EcadStationSeries> SelectSeries(
        EcadStation station,
        EcadStationMatchOptions options,
        out List<DataType> missingDataTypes,
        out List<DataType> ambiguousDataTypes)
    {
        var selected = new Dictionary<DataType, EcadStationSeries>();
        missingDataTypes = [];
        ambiguousDataTypes = [];

        foreach (var dataType in EcadConstants.PublishedDataTypes)
        {
            var series = station.GetCurrentSeries(EcadConstants.GetParameterPrefix(dataType), options.ObservedOnOrAfter);
            switch (series.Count)
            {
                case 0:
                    missingDataTypes.Add(dataType);
                    break;
                case 1:
                    selected.Add(dataType, series[0]);
                    break;
                default:
                    ambiguousDataTypes.Add(dataType);
                    break;
            }
        }

        return selected;
    }

    private static DateOnly GetLatestObservation(EcadStation station)
    {
        return station.Series.Count == 0 ? DateOnly.MinValue : station.Series.Max(x => x.LastDate);
    }

    private static string Describe(IEnumerable<RankedCandidate> candidates)
    {
        return string.Join("; ", candidates.Select(Describe));
    }

    private static string Describe(RankedCandidate candidate)
    {
        return $"{candidate.Candidate.Station.Id} '{candidate.Candidate.Station.Name}' " +
            $"{candidate.DistanceKm:0.00}km name={candidate.NameSimilarity:0.00}";
    }

    private static string Describe(UsableStation candidate)
    {
        return $"{candidate.Station.Id} '{candidate.Station.Name}'";
    }

    private sealed record UsableStation(
        EcadStation Station,
        IReadOnlyDictionary<DataType, EcadStationSeries> Series,
        IReadOnlyList<DataType> MissingDataTypes,
        IReadOnlyList<DataType> AmbiguousDataTypes)
    {
        public bool IsUsable => MissingDataTypes.Count == 0 && AmbiguousDataTypes.Count == 0;
    }

    private sealed record RankedCandidate(UsableStation Candidate, double DistanceKm, double NameSimilarity);
}

public sealed record EcadStationMatchOptions
{
    /// <summary>
    /// GHCN and ECA&amp;D publish their own coordinates for the same site and they disagree by up to a
    /// couple of kilometres, so the tolerance has to be looser than the disagreement while staying tight
    /// enough that the name check is deciding between plausible candidates rather than a whole city.
    /// </summary>
    public double MaximumDistanceKm { get; init; } = 5d;

    /// <summary>
    /// How alike two station names must be before proximity is believed. GHCN and ECA&amp;D name the same
    /// site differently often enough ("BOURNEMOUTH" against "Hurn") that some real matches are lost here;
    /// that is the intended trade, since the alternative is silently attaching a location to its neighbour.
    /// </summary>
    public double MinimumNameSimilarity { get; init; } = 0.6d;

    /// <summary>
    /// How recently a series must have reported to count as live. ECA&amp;D is declared ahead of GHCNd, so
    /// it takes over a location entirely - matching a station whose series ended years ago would replace
    /// working GHCNd data with a dead series.
    /// </summary>
    public required DateOnly ObservedOnOrAfter { get; init; }
}

public sealed record EcadStationMatchReport(
    IReadOnlyList<EcadStationMatch> Matches,
    IReadOnlyList<EcadStationRejection> Rejections);

public sealed record EcadStationMatch(
    string GhcnStationId,
    string EcadStationId,
    string? EcadStationName,
    double DistanceKm,
    double NameSimilarity,
    EcadStationMatchKind Kind,
    IReadOnlyDictionary<DataType, EcadStationSeries> Series)
{
    public IReadOnlyDictionary<DataType, string> ParameterCodes =>
        Series.ToDictionary(x => x.Key, x => x.Value.ParameterCode);

    /// <summary>
    /// The first day any of the station's selected series reports. Bootstrapping from here rather than
    /// from an arbitrarily early date keeps most stations inside a single request, which matters because
    /// the API's request quota is the binding constraint on a full build.
    /// </summary>
    public DateOnly FirstObservation => Series.Values.Min(x => x.FirstDate);
}

public sealed record EcadStationRejection(
    string GhcnStationId,
    EcadStationRejectionReason Reason,
    string Detail);

public enum EcadStationMatchKind
{
    /// <summary>One candidate scored better than every other.</summary>
    Unique,

    /// <summary>Several candidates were the same station registered by more than one participant.</summary>
    DuplicateRegistration,
}

public enum EcadStationRejectionReason
{
    /// <summary>Nothing nearby was named closely enough to believe it is the same station.</summary>
    NameNotCorroborated,

    /// <summary>Two differently-named stations were equally good matches.</summary>
    Ambiguous,

    /// <summary>The station does not currently report every measurement ClimateExplorer publishes.</summary>
    IncompleteMeasurements,

    /// <summary>
    /// The station currently reports two accumulation-period variants of the same measurement, so there is
    /// no single canonical series to publish.
    /// </summary>
    AmbiguousParameterVariant,

    /// <summary>The chosen ECA&amp;D station was also the best match for a different GHCN station.</summary>
    Contested,
}
