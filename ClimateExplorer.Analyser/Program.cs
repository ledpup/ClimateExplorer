using ClimateExplorer.Analyser;
using ClimateExplorer.Analyser.Bom;
using ClimateExplorer.Analyser.Greenland;
using ClimateExplorer.Analyser.Niwa;
using ClimateExplorer.Analyser.StaticContent;
using ClimateExplorer.Core.Model;

var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

var httpClient = new HttpClient();
var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

GenerateMapMarkers();
await BuildStaticContent.GenerateSiteMap();
await BuildStaticContent.GenerateIndexFiles();

await GreenlandApiClient.GetMeltDataAndSave(httpClient);

var referenceDataDefintions = dataSetDefinitions
    .Where(x =>
            !string.IsNullOrEmpty(x.DataDownloadUrl)
            && x.MeasurementDefinitions.Count == 1
            && x.Id != Guid.Parse("6484A7F8-43BC-4B16-8C4D-9168F8D6699C") // Greenland is dealt with as a special case, see GreenlandApiClient
            )
    .ToList();

foreach ( var dataSetDefinition in referenceDataDefintions)
{
    await DownloadDataSetData(dataSetDefinition);
}

var niwaStations = await Station.GetStationsFromFiles(
    new List<string> 
    { 
        $@"ReferenceMetaData\NIWA\Stations_NewZealand_7stations_unadjusted.json",
        $@"ReferenceMetaData\NIWA\Stations_NewZealand_11stations.json",
    });

await NiwaCliFloClient.GetDataForEachStation(niwaStations);

// await ValidateLocations();

await NiwaLocationsAndStationsMapper.BuildNiwaLocationsAsync(Guid.Parse("7522E8EC-E743-4CB0-BC65-6E9F202FC824"), "7-stations_locations_adjusted.csv", "7-stations_Locations.json", "_NewZealand_7stations_adjusted");
await NiwaLocationsAndStationsMapper.BuildNiwaLocationsAsync(Guid.Parse("534950DC-EDA4-4DB5-8816-3705358F1797"), "7-stations_locations_unadjusted.csv", "7-stations_Locations.json", "_NewZealand_7stations_unadjusted");
await NiwaLocationsAndStationsMapper.BuildNiwaLocationsAsync(Guid.Parse("88e52edd-3c67-484a-b614-91070037d47a"), "11-stations_locations.csv", "11-stations_Locations.json", "_NewZealand_11stations");

var stations = await BomLocationsAndStationsMapper.BuildAcornSatLocationsFromReferenceMetaDataAsync(Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"), "_Australia_unadjusted");
await BomDataDownloader.GetDataForEachStation(httpClient, stations);
await BomLocationsAndStationsMapper.BuildAcornSatAdjustedDataFileLocationMappingAsync(Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"), @"Output\DataFileLocationMapping\DataFileLocationMapping_Australia_unadjusted.json", "_Australia_adjusted");

await BomLocationsAndStationsMapper.BuildRaiaLocationsFromReferenceMetaDataAsync(Guid.Parse("647b6a05-43e4-48e0-a43e-04ae81a74653"), "_Australia_Raia");

async Task ValidateLocations()
{
    var locations = await Location.GetLocations(@"Output\Location");

    if (locations.GroupBy(x => x.Id).Any(x => x.Count() > 1))
    {
        throw new Exception("There are duplicate location IDs");
    }
    if (locations.GroupBy(x => x.Name).Any(x => x.Count() > 1))
    {
        throw new Exception("There are duplicate location names");
    }
}

async Task DownloadDataSetData(DataSetDefinition dataSetDefinition)
{
    var md = dataSetDefinition.MeasurementDefinitions.Single();
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
        "#053061",
        "#2166AC",

        "#ffffff",
        "#ffffff",
        "#ffffd0",
        "#ffffd0",
        "#e4bd7d",
        "#e4bd7d",
        "#ce7642",
        "#b2182b",
        "#67001f",

        "#007FFF",
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