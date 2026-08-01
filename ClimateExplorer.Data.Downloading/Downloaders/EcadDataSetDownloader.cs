namespace ClimateExplorer.Data.Downloading.Downloaders;

using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Downloading.Workspace;
using ClimateExplorer.Data.Ecad;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using static ClimateExplorer.Core.Enums;

/// <summary>
/// Keeps an ECA&amp;D station's published series up to date. Following the <see cref="GreenlandDataSetDownloader"/>
/// precedent, it reads whatever was last published to work out what it still needs, fetches only that, and
/// writes the merged result - the station's history reaches back centuries and re-fetching it on every
/// refresh would be both wasteful and, given the API's query size limit, several requests per station.
/// </summary>
public sealed class EcadDataSetDownloader(
    EcadApiClient client,
    DataSetSourceFileStore sourceFileStore,
    IReadOnlyDictionary<string, string> ghcnIdToEcadStationId,
    ILogger<EcadDataSetDownloader>? logger = null) : IDataSetDownloader
{
    private readonly EcadApiClient client = client;
    private readonly DataSetSourceFileStore sourceFileStore = sourceFileStore;
    private readonly IReadOnlyDictionary<string, string> ghcnIdToEcadStationId = ghcnIdToEcadStationId;
    private readonly ILogger logger = logger ?? NullLogger<EcadDataSetDownloader>.Instance;

    public string Key => "ecad-station";

    public async Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryDirectory);

        var stationIds = request.Measurements.Select(x => x.FileFilter.Id).Distinct(StringComparer.Ordinal).ToList();
        if (stationIds.Count != 1)
        {
            throw new InvalidOperationException("An ECA&D source asset must resolve to exactly one station.");
        }

        // Everything ClimateExplorer stores about the station is keyed by GHCN id; only the API call needs
        // ECA&D's own id, and the crosswalk is the single place the two are related.
        var ghcnStationId = stationIds[0];
        if (!ghcnIdToEcadStationId.TryGetValue(ghcnStationId, out var ecadStationId))
        {
            throw new InvalidDataException($"No ECA&D station is mapped to GHCN station '{ghcnStationId}'.");
        }

        var published = ReadPublishedObservations(request.RelativePath);
        var requestedDataTypes = request.Measurements
            .Select(x => x.MeasurementDefinition.DataType)
            .Distinct()
            .ToList();

        var from = published.Count == 0
            ? EcadHistoryRange.Earliest
            : published.Keys.Max().AddDays(1);
        var to = DateOnly.FromDateTime(DateTime.UtcNow);

        if (from <= to)
        {
            var fetched = await FetchAsync(ecadStationId, ghcnStationId, requestedDataTypes, from, to, cancellationToken);
            foreach (var observation in fetched)
            {
                published[observation.Date] = observation;
            }
        }

        if (published.Count == 0)
        {
            throw new InvalidDataException(
                $"ECA&D returned no observations for station '{ghcnStationId}' ({ecadStationId}) and nothing was previously published.");
        }

        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        await EcadStationArchiveBuilder.BuildAsync(published.Values, ghcnStationId, candidatePath, cancellationToken);
        return new DataSetDownloadArtifact(candidatePath);
    }

    private async Task<IReadOnlyList<EcadDailyObservation>> FetchAsync(
        string ecadStationId,
        string ghcnStationId,
        IReadOnlyCollection<DataType> dataTypes,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        // Which numbered accumulation-period variant a station reports varies by contributing country and
        // can change, so the whole candidate family is requested rather than a remembered code. The window
        // is only ever a handful of days, which leaves the request far inside the API's size limit even
        // with every variant listed.
        var candidateCodes = new List<string>();
        var codesByDataType = new Dictionary<DataType, IReadOnlyList<string>>();
        foreach (var dataType in dataTypes)
        {
            var codes = await client.GetParameterNamesAsync(dataType, cancellationToken);
            codesByDataType.Add(dataType, codes);
            candidateCodes.AddRange(codes);
        }

        var observations = await client.GetObservationsAsync(ecadStationId, candidateCodes, from, to, cancellationToken);

        var rows = new SortedDictionary<DateOnly, EcadDailyObservation>();
        foreach (var (dataType, codes) in codesByDataType)
        {
            var populated = codes.Where(x => observations.ContainsKey(x)).ToList();
            if (populated.Count == 0)
            {
                continue;
            }

            if (populated.Count > 1)
            {
                // The one-convention-per-station assumption this integration is built on has broken for
                // this station. Rather than pick silently, take the best-covered variant and say so, so a
                // real tie-break rule can be designed if it ever starts happening.
                logger.LogWarning(
                    "ECA&D station {EcadStationId} ({GhcnStationId}) reported {Count} {DataType} variants ({Codes}) between {From} and {To}; using the best covered.",
                    ecadStationId,
                    ghcnStationId,
                    populated.Count,
                    dataType,
                    string.Join(", ", populated),
                    from,
                    to);
            }

            var selected = populated
                .OrderByDescending(x => observations[x].Count)
                .ThenBy(x => x, StringComparer.Ordinal)
                .First();

            foreach (var (date, value) in observations[selected])
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

    private SortedDictionary<DateOnly, EcadDailyObservation> ReadPublishedObservations(string relativePath)
    {
        var publishedPath = sourceFileStore.ResolvePath(relativePath);
        return File.Exists(publishedPath)
            ? EcadCsvFormat.Read(EcadStationArchiveBuilder.ReadArchive(publishedPath))
            : [];
    }
}
