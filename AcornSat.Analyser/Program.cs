using AcornSat.Core.InputOutput;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;
using AcornSat.Core;
using static AcornSat.Core.Enums;

BuildLocationsFromReferenceData();
BuildDataSetDefinition();

var date = new DateTime(1980, 1, 1);

for (var i = 0; i < 50; i++)
{
    date = date.AddMonths(1);

    Console.WriteLine(date);
}

Console.WriteLine();


void BuildDataSetDefinition()
{
    var ddd = new List<DataSetDefinition>
    {
        new DataSetDefinition
        {
            Id = Guid.NewGuid(),
            Name = "ACORN-SAT",
            Description = "The Australian Climate Observations Reference Network - Surface Air Temperature data set, is a homogenized daily maximum and minimum temperature data set containing data from 112 locations across Australia extending from 1910 to the present.",
            FolderName = "ACORN-SAT",
            DataType = DataType.Temperature,
            DataResolution = DataResolution.Daily,
            DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<temperature>-?\d*\.*\d*).*$",
            MaxTempFolderName = "daily_tmax",
            MinTempFolderName = "daily_tmin",
            MaxTempFileName = "tmax.[station].daily.csv",
            MinTempFileName = "tmin.[station].daily.csv",
        },
        new DataSetDefinition
        {
            Id = Guid.NewGuid(),
            Name = "RAIA",
            Description = "This ACORN-SAT dataset includes homogenised monthly data from the Remote Australian Islands and Antarctica network of 8 locations, which provide ground-based temperature records.",
            FolderName = "RAIA",
            DataType = DataType.Temperature,
            DataResolution = DataResolution.Monthly,
            DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<temperature>-?\d+\.\d+)$",
            MaxTempFolderName = "maxT",
            MinTempFolderName = "minT",
            MaxTempFileName = "acorn.ria.maxT.[station].monthly.txt",
            MinTempFileName = "acorn.ria.minT.[station].monthly.txt",
            NullValue = "99999.9",
        }
    };

    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText("DataSetDefinitions.json", JsonSerializer.Serialize(ddd, options));
}





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

