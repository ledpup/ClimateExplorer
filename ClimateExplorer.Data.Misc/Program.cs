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

var httpClient = CreateHttpClient();

var dataSetSourceAssetResolver = new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
var timeProvider = TimeProvider.System;
var dataSetHttpFileDownloader = new DataSetHttpFileDownloader(httpClient);
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
        new DirectHttpDataSetDownloader(dataSetHttpFileDownloader),
        new GhcndDataSetDownloader(GhcndHttpClientFactory.CreateHttpClient()),
        new BomDataSetDownloader(new BomDailyDataClient(httpClient)),
        new NoaaGlobalTempDataSetDownloader(dataSetHttpFileDownloader, timeProvider),
        new GreenlandDataSetDownloader(new GreenlandMeltDataClient(httpClient), dataSetSourceFileStore, timeProvider),
        new TransformingDataSetDownloader("ocean-acidity", dataSetHttpFileDownloader, new OceanAciditySourceFileTransformer()),
        new TransformingDataSetDownloader("sea-level", dataSetHttpFileDownloader, new SeaLevelSourceFileTransformer()),
        new TransformingDataSetDownloader("ozone", dataSetHttpFileDownloader, new OzoneSourceFileTransformer()),
    ],
    timeProvider,
    factory.CreateLogger<DataSetSourceUpdateCoordinator>());
IDataSetBatchRefresher dataSetBatchRefresher = new DataSetBatchRefresher(
    dataSetSourceAssetResolver,
    dataSetSourceUpdateCoordinator);
await dataSetBatchRefresher.RefreshAllAsync();

var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

var stations = NoaaGlobalTemp.DataFileMapping().LocationIdToDataFileMappings.Values.Select(x => x.Single().Id);

File.WriteAllText($@"{Folders.MetaDataFolder}\Station\Stations_NoaaGlobalTemp.json", JsonSerializer.Serialize(stations.Select(x => new Station { Id = x }), jsonSerializerOptions));
File.WriteAllText($@"{Folders.MetaDataFolder}\DataFileMapping\DataFileMapping_NoaaGlobalTemp.json", JsonSerializer.Serialize(NoaaGlobalTemp.DataFileMapping(), jsonSerializerOptions));

await BuildStaticContent.GenerateSiteMap();
GenerateMapMarkers();

static void GenerateMapMarkers()
{
    var fillColours = new List<string>
    {
        "#053061", // negative (deep cold blue)

        "#c6dbe0", // 0 (light blue)
        "#deebf7", // 1 (pale blue)
        "#ffefef", // 2
        "#fee8c8", // 3 (warm cream)
        "#fdd49e", // 4 (light orange)
        "#fdbb84", // 5 (orange)
        "#fc8d59", // 6 (orange-red)
        "#ef6548", // 7 (red-orange)
        "#d7301f", // 8 (strong red)
        "#67001f", // 9 (deep hot red)

        "#D0D0D0", // ? (neutral/unknown)
    };

    var textColours = new List<string>
    {
        "#ffffff", // negative (dark fill → white text)

        "#333333", // 0 (light fill → dark text)
        "#333333", // 1
        "#333333", // 2
        "#333333", // 3
        "#333333", // 4
        "#333333", // 5
        "#ffffff", // 6
        "#ffffff", // 7
        "#ffffff", // 8
        "#ffffff", // 9

        "#333333", // ? (neutral grey → dark text)
    };

    for (var i = -1; i < 11; i++)
    {
        var svg = File.ReadAllText("MapMarker.svg");
        svg = svg.Replace("{colour}", fillColours[i + 1]);
        var text = i == -1
                        ? "-"
                        : i == 10
                            ? "?"
                            : i.ToString();
        svg = svg.Replace("{text}", text);
        svg = svg.Replace("{text-colour}", textColours[i + 1]);
        var fileName = i == -1
                            ? "negative"
                            : i == 10
                                    ? "null"
                                    : i.ToString();
        File.WriteAllText($"{fileName}.svg", svg);
    }
}

static HttpClient CreateHttpClient()
{
    var httpClient = new HttpClient();
    var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
    var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
    return httpClient;
}
