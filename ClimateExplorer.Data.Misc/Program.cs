#pragma warning disable SA1200 // Using directives should be placed correctly
using System.Text.Json;
using System.Text.Json.Serialization;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Downloading.Transformers;
using ClimateExplorer.Data.Downloading.Workspace;
using ClimateExplorer.Data.Ghcnd;
using ClimateExplorer.Data.Misc;
using ClimateExplorer.Data.Misc.NoaaGlobalTemp;
using Microsoft.Extensions.Logging;
#pragma warning restore SA1200 // Using directives should be placed correctly

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger("ClimateExplorer.Data.Misc");
logger.LogInformation("Starting ClimateExplorer.Data.Misc");

// "--map-markers" only regenerates the map markers, without the site map or any dataset download.
if (args.Contains("--map-markers"))
{
    GenerateMapMarkers();
    return;
}

await BuildStaticContent.GenerateSiteMap();

var httpClient = CreateHttpClient();

var dataSetSourceAssetResolver = new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
var timeProvider = TimeProvider.System;
var dataSetHttpFileDownloader = new DataSetHttpFileDownloader(httpClient, factory.CreateLogger<DataSetHttpFileDownloader>());
var dataSetSourceFileStore = new DataSetSourceFileStore(Folders.SourceDataFolder);
var dataSetSourceUpdateCoordinator = new DataSetSourceUpdateCoordinator(
    dataSetSourceAssetResolver,
    new DataSetFreshnessPolicy(timeProvider),
    new DataSetAssetLockProvider(),
    new DataSetDownloadWorkspaceFactory(),
    dataSetSourceFileStore,
    new FileDataSetSourceStateStore(Path.Combine("Output", "DataSetSourceState")),
    new DataSetDownloadValidator(),
    [

        // Only downloaders actually registered here are run - DataSetBatchRefresher skips (rather than fails)
        // any dataset whose downloader key isn't in this list, so comment lines out to limit a local run.
        new DirectHttpDataSetDownloader(dataSetHttpFileDownloader),
        new GhcndDataSetDownloader(GhcndHttpClientFactory.CreateHttpClient()),
        new BomDataSetDownloader(new BomDailyDataClient(BomHttpClientFactory.CreateBomHttpClient())),
        new NoaaGlobalTempDataSetDownloader(dataSetHttpFileDownloader, timeProvider),
        new GreenlandDataSetDownloader(new GreenlandMeltDataClient(httpClient), dataSetSourceFileStore, timeProvider),
        new TransformingDataSetDownloader("ocean-acidity", dataSetHttpFileDownloader, new OceanAciditySourceFileTransformer()),
        new TransformingDataSetDownloader("sea-level", dataSetHttpFileDownloader, new SeaLevelSourceFileTransformer()),
        new TransformingDataSetDownloader("ozone", dataSetHttpFileDownloader, new OzoneSourceFileTransformer()),
        new TransformingDataSetDownloader("wgms-glacier-mass-balance", dataSetHttpFileDownloader, new WgmsGlacierMassBalanceSourceFileTransformer(WgmsGlacierFilter.Benchmark, WgmsAveragingStage.TwoStage)),
    ],
    timeProvider,
    factory.CreateLogger<DataSetSourceUpdateCoordinator>());
IDataSetBatchRefresher dataSetBatchRefresher = new DataSetBatchRefresher(
    dataSetSourceAssetResolver,
    dataSetSourceUpdateCoordinator,
    factory.CreateLogger<DataSetBatchRefresher>());

// Defaults to always force-refreshing (see docs/operations/automated-dataset-downloads.md). Set
// FORCE_REFRESH=false for repeated local runs - e.g. iterating on a downloader/transformer - to reuse a
// still-fresh previously downloaded source instead of re-pulling a large source file every run.
var forceRefresh = Environment.GetEnvironmentVariable("FORCE_REFRESH") != "false";
await dataSetBatchRefresher.RefreshAllAsync(forceRefresh);

var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

var stations = NoaaGlobalTemp.DataFileMapping().LocationIdToDataFileMappings.Values.Select(x => x.Single().Id);

File.WriteAllText($@"{Folders.MetaDataFolder}\Station\Stations_NoaaGlobalTemp.json", JsonSerializer.Serialize(stations.Select(x => new Station { Id = x }), jsonSerializerOptions));
File.WriteAllText($@"{Folders.MetaDataFolder}\DataFileMapping\DataFileMapping_NoaaGlobalTemp.json", JsonSerializer.Serialize(NoaaGlobalTemp.DataFileMapping(), jsonSerializerOptions));

GenerateMapMarkers();

// Writes one marker per trend score into the web app, which serves the SVGs as they are. The colours
// come from TrendScoreColours, which the locations table's badges use too.
static void GenerateMapMarkers()
{
    const string outputFolder = @"..\..\..\..\ClimateExplorer.Web\wwwroot\images\map-markers\";
    var template = File.ReadAllText("MapMarker.svg");

    foreach (var score in TrendScoreColours.Scores)
    {
        var (fileName, text) = score == Location.NegativeRateTrendScore
            ? ("negative", "-")
            : (score.ToString(), score.ToString());
        WriteMapMarker(template, Path.Combine(outputFolder, $"{fileName}.svg"), text, TrendScoreColours.Get(score));
    }

    // A location without a score gets a plain grey marker, with no text.
    WriteMapMarker(template, Path.Combine(outputFolder, "null.svg"), string.Empty, TrendScoreColours.NoScore);
}

static void WriteMapMarker(string template, string path, string text, (string Background, string Text) colours)
{
    var svg = template
        .Replace("{colour}", colours.Background)
        .Replace("{text-colour}", colours.Text)
        .Replace("{text}", text);
    File.WriteAllText(path, svg);
}

static HttpClient CreateHttpClient()
{
    // Timeout must be left uncapped (or at least >= DataSetHttpFileDownloader's AttemptTimeout): HttpClient.Timeout
    // spans the whole request including the body read even with ResponseHeadersRead, and enforces itself by
    // disposing the underlying connection - which surfaces as a confusing ObjectDisposedException/IOException
    // mid-read rather than a clean cancellation, and fires long before the 5-minute per-attempt ceiling below
    // would otherwise decide a slow-but-alive download is worth retrying.
    var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
    var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
    return httpClient;
}
