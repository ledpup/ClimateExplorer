using AcornSat.Core.InputOutput;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;

//BuildLocationsFromReferenceData();
var locations = Location.GetLocations(@"..\..\..\..\AcornSat.WebApi\MetaData\ACORN-SAT\Locations.json");






void BuildLocationsFromReferenceData()
{
    var locations = new List<Location>();

    var locationRowData = File.ReadAllLines(@"ReferenceData\Locations.csv");
    foreach (var row in locationRowData)
    {
        var splitRow = row.Split(',');
        var location = new Location { Id = Guid.NewGuid(), Name = splitRow[0] };
        location.Sites.Add(splitRow[1]);
        if (splitRow.Length > 2 && !string.IsNullOrWhiteSpace(splitRow[2]))
        {
            location.Sites.Add(splitRow[2]);
        }
        locations.Add(location);
    }

    var primarySites = File.ReadAllLines(@"ReferenceData\primarysites.txt");

    var siteSets = new Dictionary<string, List<string>>();

    foreach (var primarySiteRow in primarySites)
    {
        var primarySite = primarySiteRow.Substring(0, 6);
        var firstSite = primarySiteRow.Substring(7, 6);
        var secondSite = primarySiteRow.Substring(32, 6);
        var thirdSite = primarySiteRow.Substring(57, 6);

        siteSets.Add(primarySite, new List<string>
        {
            firstSite
        });

        if (secondSite != "999999")
        {
            siteSets[primarySite].Add(secondSite);
            if (thirdSite != "999999")
            {
                siteSets[primarySite].Add(thirdSite);
            }
        }

        var location = locations.Single(x => x.Sites.Contains(primarySite));
        siteSets[primarySite].ForEach(x =>
        {
            if (!location.Sites.Contains(x))
            {
                location.Sites.Add(x);
            }
        }
        );
    }

    var moreLocationData = File.ReadAllLines(@"ReferenceData\acorn_sat_v2.1.0_stations.csv");

    for (var i = 1; i < moreLocationData.Length; i++)
    {
        var splitRow = moreLocationData[i].Split(',');
        var id = splitRow[0].PadLeft(6, '0');

        var location = locations.Single(x => x.Sites.Contains(id));

        if (location.Name != splitRow[1])
        {
            Console.WriteLine($"Location name mismatch. '{location.Name}' not equal to '{splitRow[1]}'");
        }

        location.Coordinates = new Coordinates
        {
            Latitude = float.Parse(splitRow[2]),
            Longitude = float.Parse(splitRow[3]),
            Elevation = float.Parse(splitRow[4]),
        };
    }

    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText("locations.json", JsonSerializer.Serialize(locations, options));
}

