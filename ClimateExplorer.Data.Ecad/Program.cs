using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ecad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Builds ClimateExplorer's ECA&D coverage: which locations ECA&D can serve, which ECA&D station serves
// each one, and each station's full published history. Run manually and periodically, like the other
// ClimateExplorer.Data.* tools; the runtime downloader only ever extends what this produces.
var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder => loggingBuilder
        .SetMinimumLevel(LogLevel.Information)
        .AddSimpleConsole(x => { x.SingleLine = true; x.IncludeScopes = false; }))
    .BuildServiceProvider();
var logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger<Program>();

using var httpClient = EcadApiClient.CreateHttpClient();
var client = new EcadApiClient(httpClient, logger);
var cancellationToken = CancellationToken.None;

// A station cap makes a smoke run against the live API cheap; a full run publishes every match, so it is
// the only mode that should ever produce the checked-in metadata.
var stationLimit = EcadBuildSettings.ReadStationLimit(args);

var ghcnStations = await Station.GetStationsFromFile(Folders.GhcnStationMetadataFile);
logger.LogInformation("Loaded {Count} GHCN stations.", ghcnStations.Count);

logger.LogInformation("Fetching the ECA&D non-blended station listing.");
var ecadStations = await client.GetStationsAsync(cancellationToken);
logger.LogInformation("ECA&D published {Count} stations.", ecadStations.Count);

// A station only counts as live if it is still reporting. ECA&D's own publication lag means "still
// reporting" has to be measured against the collection's latest observation rather than against today.
var latestObservation = ecadStations.SelectMany(x => x.Series).Max(x => x.LastDate);
var observedOnOrAfter = latestObservation.AddDays(-EcadBuildSettings.LiveSeriesToleranceDays);
logger.LogInformation(
    "ECA&D's latest observation is {LatestObservation}; requiring series to report on or after {ObservedOnOrAfter}.",
    latestObservation,
    observedOnOrAfter);

var report = EcadStationMatcher.Match(
    ghcnStations,
    ecadStations,
    new EcadStationMatchOptions { ObservedOnOrAfter = observedOnOrAfter });

// Most rejections are locations where ECA&D has a station that simply stopped reporting - true, but not
// actionable, and there are hundreds of them. The ones a maintainer might do something about (a real
// station that could not be told apart from its neighbour, or whose name did not corroborate) are called
// out individually; the rest are counted.
foreach (var rejection in report.Rejections.Where(x => x.Reason is not EcadStationRejectionReason.IncompleteMeasurements))
{
    logger.LogInformation("Skipping {GhcnStationId}: {Reason}. {Detail}", rejection.GhcnStationId, rejection.Reason, rejection.Detail);
}

foreach (var reason in report.Rejections.GroupBy(x => x.Reason).OrderBy(x => x.Key))
{
    logger.LogInformation("Skipped {Count} station(s) for {Reason}.", reason.Count(), reason.Key);
}

foreach (var match in report.Matches.Where(x => x.Kind == EcadStationMatchKind.DuplicateRegistration))
{
    logger.LogInformation(
        "{GhcnStationId} matched {EcadStationId} '{EcadStationName}', chosen from several registrations of the same station by latest observation.",
        match.GhcnStationId,
        match.EcadStationId,
        match.EcadStationName);
}

logger.LogInformation("Matched {Matched} stations; skipped {Skipped}.", report.Matches.Count, report.Rejections.Count);
if (report.Matches.Count == 0)
{
    logger.LogError("No stations matched, so there is nothing to publish. Leaving the existing metadata alone.");
    return 1;
}

var datasetsFolder = Path.Combine(Folders.MetaDataFolder, "..", "Datasets", "Ecad", "Unadjusted");
var sourceDataFolder = Path.Combine(Folders.SourceDataFolder, "Ecad", "Unadjusted");
Directory.CreateDirectory(datasetsFolder);
Directory.CreateDirectory(sourceDataFolder);

var published = new List<EcadStationMatch>();
var failures = new List<string>();
var stationsToPublish = stationLimit.HasValue ? report.Matches.Take(stationLimit.Value).ToList() : report.Matches;
if (stationLimit.HasValue)
{
    logger.LogWarning(
        "Publishing only the first {Limit} of {Total} matched stations. The metadata this writes is a smoke test, not a release.",
        stationLimit.Value,
        report.Matches.Count);
}

foreach (var match in stationsToPublish)
{
    try
    {
        var observations = await EcadStationHistoryLoader.LoadAsync(client, match, cancellationToken);
        var archivePath = Path.Combine(datasetsFolder, $"{match.GhcnStationId}.zip");
        await EcadStationArchiveBuilder.BuildAsync(observations, match.GhcnStationId, archivePath, cancellationToken);

        // The dataset folder is what the site serves and the downloader updates; the source data folder is
        // the checked-in copy the test suite validates every asset against. Both need every station.
        File.Copy(archivePath, Path.Combine(sourceDataFolder, $"{match.GhcnStationId}.zip"), true);

        published.Add(match);
        logger.LogInformation(
            "Published {GhcnStationId} from {EcadStationId} ({Days} days, {First} to {Last}).",
            match.GhcnStationId,
            match.EcadStationId,
            observations.Count,
            observations.Count == 0 ? null : observations.Min(x => x.Date),
            observations.Count == 0 ? null : observations.Max(x => x.Date));
    }
    catch (Exception exception) when (exception is InvalidDataException or HttpRequestException)
    {
        // One station's data being unusable should not cost the other 200 their refresh; it is dropped
        // from the mapping so the runtime never asks for a file that was never written.
        failures.Add(match.GhcnStationId);
        logger.LogError(
            "Could not publish {GhcnStationId} from {EcadStationId}: {Message}. It will be excluded.",
            match.GhcnStationId,
            match.EcadStationId,
            exception.Message);

        // Individual stations failing is tolerable; a run in which most of them fail is not a release,
        // it is an outage. Writing the mapping anyway would silently drop working locations off the site,
        // which is exactly what a rate-limited run did before this check existed.
        if (failures.Count > EcadBuildSettings.MaximumFailuresBeforeAbandoning)
        {
            logger.LogError(
                "{Failed} stations failed, which is too many to treat as isolated problems. Abandoning the run and leaving the existing metadata alone.",
                failures.Count);
            return 1;
        }
    }
}

// Only once the run has succeeded, and never for a capped smoke run, which is not a complete picture of
// what should exist. A station that stops qualifying - its series ends, or it starts reporting two
// variants of a measurement - drops out of the mapping, and its archive would otherwise sit there
// unreferenced forever.
if (!stationLimit.HasValue)
{
    var expected = published.Select(x => $"{x.GhcnStationId}.zip").ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var folder in new[] { datasetsFolder, sourceDataFolder })
    {
        foreach (var path in Directory.EnumerateFiles(folder, "*.zip").Where(x => !expected.Contains(Path.GetFileName(x))))
        {
            logger.LogInformation("Removing {Path}; its station is no longer mapped to ECA&D.", path);
            File.Delete(path);
        }
    }
}

await EcadStationCrosswalk.SaveAsync(
    Path.Combine(Folders.MetaDataFolder, EcadStationCrosswalk.FileName),
    published,
    cancellationToken);

await EcadDataFileMappingBuilder.CreateDataFileMappingAsync(
    published,
    await EcadDataFileMappingBuilder.GetGhcnIdToLocationIdsAsync(cancellationToken),
    Path.Combine(Folders.MetaDataFolder, "DataFileMapping", EcadDataFileMappingBuilder.FileName),
    cancellationToken);

logger.LogInformation("Published {Published} stations; {Failed} could not be published.", published.Count, failures.Count);
return 0;

internal static class EcadBuildSettings
{
    /// <summary>
    /// How far behind the collection's own latest observation a station's series may fall and still count
    /// as live. Contributing services report on their own cadences, so a few weeks of slack keeps stations
    /// that simply submit less often, while still excluding series that have genuinely stopped.
    /// </summary>
    public const int LiveSeriesToleranceDays = 31;

    /// <summary>
    /// How many stations may fail before the run is treated as an outage rather than a set of isolated
    /// problems. A handful of stations whose data will not parse is a normal, publishable result; dozens
    /// means the source is unreachable or throttling, and publishing a mapping built from what did get
    /// through would quietly remove working locations from the site.
    /// </summary>
    public const int MaximumFailuresBeforeAbandoning = 10;

    public static int? ReadStationLimit(string[] args)
    {
        const string option = "--max-stations";
        var index = Array.IndexOf(args, option);
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out var limit) || limit <= 0)
        {
            throw new ArgumentException($"{option} requires a positive station count.");
        }

        return limit;
    }
}
