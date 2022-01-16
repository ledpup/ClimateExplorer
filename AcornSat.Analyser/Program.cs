using AcornSat.Core.InputOutput;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;
using AcornSat.Core;
using static AcornSat.Core.Enums;

BuildDataSetDefinitions();
BuildNiwaLocations(Guid.NewGuid());
BuildAcornSatLocationsFromReferenceData();


var date = new DateTime(1980, 1, 1);

for (var i = 0; i < 50; i++)
{
    date = date.AddMonths(1);

    Console.WriteLine(date);
}

Console.WriteLine();


void BuildDataSetDefinitions()
{
    var dataSetDefinitions = new List<DataSetDefinition>
    {
        new DataSetDefinition
        {
            Id = Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"),
            Name = "ACORN-SAT",
            Description = "The Australian Climate Observations Reference Network - Surface Air Temperature data set, is a homogenized daily maximum and minimum temperature data set containing data from 112 locations across Australia extending from 1910 to the present.",
            MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
            FolderName = "ACORN-SAT",
            MeasurementTypes = new List<MeasurementType> { MeasurementType.Adjusted, MeasurementType.Unadjusted },
            DataType = DataType.Temperature,
            DataResolution = DataResolution.Daily,
            DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<temperature>-?\d*\.*\d*).*$",
            MaxTempFolderName = "daily_tmax",
            MinTempFolderName = "daily_tmin",
            MaxTempFileName = "tmax.[station].daily.csv",
            MinTempFileName = "tmin.[station].daily.csv",
            RawDataRowRegEx = @"^\s*(?<station>\d{6})(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})\s+(?<tmax>-?\d+)\s+(?<tmin>-?\d+)$",
            RawFolderName = "raw-data",
            RawFileName = "hqnew[station].txt",
            RawNullValue = "-999",
            RawTemperatureConversion = ConversionMethod.DivideBy10,
            StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml"
        },
        new DataSetDefinition
        {
            Id = Guid.Parse("647b6a05-43e4-48e0-a43e-04ae81a74653"),
            Name = "RAIA",
            Description = "This ACORN-SAT dataset includes homogenised monthly data from the Remote Australian Islands and Antarctica network of 8 locations, which provide ground-based temperature records.",
            MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
            FolderName = "RAIA",
            MeasurementTypes = new List<MeasurementType> { MeasurementType.Adjusted },
            DataType = DataType.Temperature,
            DataResolution = DataResolution.Monthly,
            DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<temperature>-?\d+\.\d+)$",
            MaxTempFolderName = "maxT",
            MinTempFolderName = "minT",
            MaxTempFileName = "acorn.ria.maxT.[station].monthly.txt",
            MinTempFileName = "acorn.ria.minT.[station].monthly.txt",
            NullValue = "99999.9",
            StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml"
        },
        new DataSetDefinition
        {
            Id = Guid.Parse("88e52edd-3c67-484a-b614-91070037d47a"),
            Name = "NIWA 11-stations series",
            Description = "The National Institute of Water and Atmospheric Research (NIWA) eleven-station series are New Zealand temperature trends from a set of eleven climate stations with no significant site changes since the 1930s.",
            MoreInformationUrl = "https://niwa.co.nz/our-science/climate/information-and-resources/nz-temp-record/temperature-trends-from-raw-data",
            FolderName = "NIWA",
            MeasurementTypes = new List<MeasurementType> { MeasurementType.Unadjusted },
            DataType = DataType.Temperature,
            DataResolution = DataResolution.Daily,
            RawDataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
            RawFolderName = "raw-data",
            RawFileName = "[station].csv",
            RawNullValue = "-",
            RawTemperatureConversion = ConversionMethod.Unchanged,
        }
    };

    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText("DataSetDefinitions.json", JsonSerializer.Serialize(dataSetDefinitions, options));
}



void BuildNiwaLocations(Guid dataSetId)
{
    var locations = new List<Location>();
    
    var regEx = new Regex(@"^(?<name>[\w|\s|,]*),(?<station>\d+),\w\d+\w?,(?<lat>-?\d+\.\d+),(?<lng>-?\d+\.\d+),(?<alt>-?\d+).*$");
    var locationRowData = File.ReadAllLines(@"ReferenceData\NIWA\Locations.csv");

    foreach (var row in locationRowData)
    {
        var match = regEx.Match(row);
        var location = new Location
        {
            Name = match.Groups["name"].Value,
            Sites = new List<string> { match.Groups["station"].Value },
            Coordinates = new Coordinates
            {
                Latitude = float.Parse(match.Groups["lat"].Value),
                Longitude = float.Parse(match.Groups["lng"].Value),
                Elevation = float.Parse(match.Groups["alt"].Value),
            }
        };

        locations.Add(location);
    }

    locations = locations
        .GroupBy(x => x.Name)
        .Select(x => new Location
        {
            Id = Guid.NewGuid(),
            DataSetId = dataSetId,
            Name = x.Key,
            Sites = x.SelectMany(x => x.Sites).ToList(),
            Coordinates = new Coordinates
            {
                Latitude = x.ToList().Average(x => x.Coordinates.Latitude),
                Longitude = x.ToList().Average(x => x.Coordinates.Longitude),
                Elevation = x.ToList().Average(x => x.Coordinates.Elevation),
            }
        }).ToList();

    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText("niwa-locations.json", JsonSerializer.Serialize(locations, options));
}

void BuildAcornSatLocationsFromReferenceData()
{
    var locations = new List<Location>();

    var locationRowData = File.ReadAllLines(@"ReferenceData\ACORN-SAT\Locations.csv");
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

    var primarySites = File.ReadAllLines(@"ReferenceData\ACORN-SAT\primarysites.txt");

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

    var moreLocationData = File.ReadAllLines(@"ReferenceData\ACORN-SAT\acorn_sat_v2.1.0_stations.csv");

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

