using AcornSat.Core.InputOutput;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;
using AcornSat.Core;
using static AcornSat.Core.Enums;
using System.Net;
using System.IO.Compression;
using AcornSat.Core.Model;
using AcornSat.Core.ViewModel;
using System.Globalization;
using AcornSat.Analyser;
using System.Text.Json.Serialization;

GenerateMapMarkers();

var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

await NiwaLocationsAndStationsMapper.BuildNiwaLocationsAsync(Guid.Parse("7522E8EC-E743-4CB0-BC65-6E9F202FC824"), "7-stations_locations_adjusted.csv", "7-stations_Locations.json");

var stations = await BomLocationsAndStationsMapper.BuildAcornSatLocationsFromReferenceDataAsync(Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"));

await BomDataDownloader.GetDataForEachStation(stations);

async Task DownloadDataSetData(DataSetDefinition dataSetDefinition)
{
    var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
    var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
    using (var httpClient = new HttpClient())
    {
        var fileUrl = dataSetDefinition.DataDownloadUrl;
        var response = await httpClient.GetAsync(fileUrl);
        using (var fs = new FileStream(dataSetDefinition.MeasurementDefinitions.First().FileNameFormat, FileMode.OpenOrCreate))
        {
            await response.Content.CopyToAsync(fs);
        }
    }
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



