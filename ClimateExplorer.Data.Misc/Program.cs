using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Misc;
using ClimateExplorer.Data.Misc.Ozone;

var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

var httpClient = new HttpClient();
var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

OzoneFileReducer.Process("cams_ozone_monitoring_sh_ozone_area");
OzoneFileReducer.Process("cams_ozone_monitoring_sh_ozone_minimum");

await BuildStaticContent.GenerateSiteMap();
GenerateMapMarkers();

await GreenlandApiClient.GetMeltDataAndSave(httpClient);

var referenceDataDefintions = dataSetDefinitions
    .Where(x =>
            !string.IsNullOrEmpty(x.DataDownloadUrl)
            && x.MeasurementDefinitions!.Count == 1
            && x.Id != Guid.Parse("6484A7F8-43BC-4B16-8C4D-9168F8D6699C") // Greenland is dealt with as a special case, see GreenlandApiClient
            )
    .ToList();

foreach ( var dataSetDefinition in referenceDataDefintions)
{
    await DownloadDataSetData(dataSetDefinition);
}

async Task DownloadDataSetData(DataSetDefinition dataSetDefinition)
{
    var md = dataSetDefinition.MeasurementDefinitions!.Single();
    var outputPath = $@"Output\Data\{md.FolderName}";

    Directory.CreateDirectory(outputPath);

    var filePathAndName = $@"{outputPath}\{md.FileNameFormat}";
    var fi = new FileInfo(filePathAndName);
    if (fi.Exists)
    {
        return;
    }

    var fileUrl = dataSetDefinition.DataDownloadUrl;
    var response = await httpClient.GetAsync(fileUrl);
    
    var content = await response.Content.ReadAsStringAsync();

    await File.WriteAllTextAsync(filePathAndName, content);
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