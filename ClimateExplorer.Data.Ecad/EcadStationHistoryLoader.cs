namespace ClimateExplorer.Data.Ecad;

/// <summary>
/// Fetches a matched station's whole published history and folds it into daily rows. Bootstrapping the
/// full range is only ever done here, in the offline tool - the runtime downloader asks for the days after
/// whatever is already published, which is why it never approaches the API's query size limit.
/// </summary>
public static class EcadStationHistoryLoader
{
    public static async Task<IReadOnlyList<EcadDailyObservation>> LoadAsync(
        EcadApiClient client,
        EcadStationMatch match,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(match);

        var parameterCodes = match.ParameterCodes.Values.Distinct(StringComparer.Ordinal).ToList();
        if (parameterCodes.Count == 0)
        {
            throw new InvalidDataException($"No ECA&D parameter codes were selected for station '{match.EcadStationId}'.");
        }

        var observations = await client.GetObservationsAsync(
            match.EcadStationId,
            parameterCodes,
            match.FirstObservation,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        return Combine(match, observations);
    }

    /// <summary>
    /// Folds the per-parameter observations returned by the API into one row per date, placing each
    /// parameter into the column of the measurement it was selected for.
    /// </summary>
    public static IReadOnlyList<EcadDailyObservation> Combine(
        EcadStationMatch match,
        IReadOnlyDictionary<string, IReadOnlyDictionary<DateOnly, double>> observationsByParameterCode)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(observationsByParameterCode);

        var rows = new SortedDictionary<DateOnly, EcadDailyObservation>();
        foreach (var (dataType, series) in match.Series)
        {
            if (!observationsByParameterCode.TryGetValue(series.ParameterCode, out var observations))
            {
                continue;
            }

            foreach (var (date, value) in observations)
            {
                if (!rows.TryGetValue(date, out var row))
                {
                    row = new EcadDailyObservation(date);
                    rows.Add(date, row);
                }

                row[dataType] = value;
            }
        }

        return [.. rows.Values];
    }
}

public static class EcadHistoryRange
{
    /// <summary>
    /// The collection's temporal extent starts in 1756; asking from a little before that costs nothing and
    /// means an older series appearing later is picked up without a code change.
    /// </summary>
    public static readonly DateOnly Earliest = new(1700, 1, 1);
}
