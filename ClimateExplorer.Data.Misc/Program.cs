#pragma warning disable SA1200 // Using directives should be placed correctly
using System.Text.Json;
using System.Text.Json.Serialization;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Misc;
using ClimateExplorer.Data.Misc.NoaaGlobalTemp;
using ClimateExplorer.Data.Misc.OceanAcidity;
using ClimateExplorer.Data.Misc.Ozone;
using Microsoft.Extensions.Logging;
#pragma warning restore SA1200 // Using directives should be placed correctly

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger("ClimateExplorer.Data.Misc");
logger.LogInformation("Starting ClimateExplorer.Data.Misc");

var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

var httpClient = CreateHttpClient();

var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

var referenceDataDefintions = dataSetDefinitions
    .Where(x =>
            !string.IsNullOrEmpty(x.DataDownloadUrl)
            && x.MeasurementDefinitions!.Count == 1
            && x.Id != Guid.Parse("6484A7F8-43BC-4B16-8C4D-9168F8D6699C") // Greenland is dealt with as a special case, see GreenlandApiClient
            && x.Id != Guid.Parse("E61C6279-EDF4-461B-BDD1-0724D21F42F3")) // NoaaGlobalTemp is handled below
    .ToList();

foreach (var dataSetDefinition in referenceDataDefintions)
{
    await DownloadDataSetData(dataSetDefinition, Helpers.SourceDataFolder);
}

var oceanAcidity = referenceDataDefintions.Single(x => x.Name == "Ocean acidity");
OceanAcidityReducer.Process("HOT_surface_CO2", oceanAcidity.MeasurementDefinitions!.Single().FolderName!);

var seaLevel = referenceDataDefintions.Single(x => x.Name == "Mean sea level");
SeaLevelFileReducer.Process("slr_sla_gbl_free_txj1j2_90", seaLevel.MeasurementDefinitions!.Single().FolderName!);

var ozoneArea = referenceDataDefintions.Single(x => x.ShortName == "Ozone hole area");
OzoneFileReducer.Process("cams_ozone_monitoring_sh_ozone_area", ozoneArea.MeasurementDefinitions!.Single().FolderName!);
var ozoneColumn = referenceDataDefintions.Single(x => x.ShortName == "Ozone hole column");
OzoneFileReducer.Process("cams_ozone_monitoring_sh_ozone_minimum", ozoneColumn.MeasurementDefinitions!.Single().FolderName!);

// Download and build Greenland ice melt
await GreenlandApiClient.GetMeltDataAndSave(httpClient, logger);

/*
 *
 * NoaaGlobalTemp:
 * Generally, you want to be downloading for the current year - 1 and the 12th month.
 * But it depends on what is currently available on the server at https://www.ncei.noaa.gov/data/noaa-global-surface-temperature/v6/access/timeseries/
 *
 * IMPORTANT: you need to update FileNameFormat property in the measurement definition (in DataSetDefinitionsBuilder.cs) of NoaaGlobalTemp so that it is the same year and month.
 *
 */
var noaaGlobalTempYear = "2024";
var noaaGlobalTempMonth = "11";

var stations = NoaaGlobalTemp.DataFileMapping().LocationIdToDataFileMappings.Values.Select(x => new Station { Id = x.Single().Id });

File.WriteAllText($@"{Helpers.MetaDataFolder}\Station\Stations_NoaaGlobalTemp.json", JsonSerializer.Serialize(stations, jsonSerializerOptions));
File.WriteAllText($@"{Helpers.MetaDataFolder}\DataFileMapping\DataFileMapping_NoaaGlobalTemp.json", JsonSerializer.Serialize(NoaaGlobalTemp.DataFileMapping(), jsonSerializerOptions));

var nNoaaGlobalTempDsd = DataSetDefinitionsBuilder.BuildDataSetDefinitions().Single(x => x.Id == Guid.Parse("E61C6279-EDF4-461B-BDD1-0724D21F42F3"));

foreach (var station in stations)
{
    logger.LogInformation($"Attempting to download data for the area '{station.Id}', for the year {noaaGlobalTempYear}, month {noaaGlobalTempMonth}");
    nNoaaGlobalTempDsd.DataDownloadUrl = nNoaaGlobalTempDsd.DataDownloadUrl!.Replace("[station]", station.Id).Replace("[year]", noaaGlobalTempYear.ToString()).Replace("[month]", noaaGlobalTempMonth);
    // await DownloadDataSetData(nNoaaGlobalTempDsd, Helpers.SourceDataFolder, station.Id);
}

await BuildStaticContent.GenerateSiteMap();
GenerateMapMarkers();

async Task DownloadDataSetData(DataSetDefinition dataSetDefinition, string? destinationBasePath = null, string? station = null)
{
    var md = dataSetDefinition.MeasurementDefinitions!.Single();
    var outputPath = Path.Combine("Output", md.FolderName!);

    Directory.CreateDirectory(outputPath);

    var fileName = station == null ? md.FileNameFormat : md.FileNameFormat!.Replace("[station]", station);
    var filePathAndName = $@"{outputPath}\{fileName}";
    if (File.Exists(filePathAndName))
    {
        logger.LogInformation($"{fileName} already exists. Will not download again. Delete the file and re-run if you want to re-download.");
    }
    else
    {
        var fileUrl = dataSetDefinition.DataDownloadUrl;
        var response = await httpClient.GetAsync(fileUrl);

        var content = await response.Content.ReadAsStringAsync();

        logger.LogInformation($"Downloading {fileName}");
        await File.WriteAllTextAsync(filePathAndName, content);
        logger.LogInformation($"File downloaded to {filePathAndName}");
    }

    var destinationPathAndFile = destinationBasePath == null ? null : Path.Combine(destinationBasePath, md.FolderName!, fileName!);
    if (destinationPathAndFile != null)
    {
        logger.LogInformation($"Will now copy file to {destinationPathAndFile}");
        File.Copy(filePathAndName, destinationPathAndFile, true);
    }
}

static void GenerateMapMarkers()
{
    var fillColours = new List<string>
    {
        "#053061", // negative
        "#2166AC", // zero

        "#ffffff", // 1
        "#ffffff",
        "#ffffd0",
        "#ffffd0",
        "#e4bd7d",
        "#e4bd7d",
        "#ce7642",
        "#b2182b",
        "#67001f", // 9

        "#D0D0D0", // ?
    };

    var textColours = new List<string>
    {
        "#ffffff",
        "#ffffff",

        "#333333",
        "#000000",
        "#666666",
        "#333333",
        "#333333",
        "#ffffff",
        "#ffffff",
        "#ffffff",
        "#ffffff",

        "#ffffff",
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