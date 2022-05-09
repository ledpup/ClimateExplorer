using AcornSat.Core.InputOutput;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;
using AcornSat.Core;
using static AcornSat.Core.Enums;
using System.Net;
using System.IO.Compression;

var dataSetDefinitions = BuildDataSetDefinitions();
BuildNiwaLocations(Guid.Parse("88e52edd-3c67-484a-b614-91070037d47a"));
var locations = BuildAcornSatLocationsFromReferenceData(Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"));

foreach (var dataSetDefinition in dataSetDefinitions)
{
    if (dataSetDefinition.Id == Guid.Parse("ffd5f5e2-d8df-4779-a7f4-f5d148505033"))
    {
        await DownloadDataSetData(dataSetDefinition);
    }
}

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

foreach (var location in locations.ToList())
{
    foreach (var site in location.Sites)
    {
        await DownloadAndExtractDailyBomData(site, ObsCode.Daily_TempMax);
        await DownloadAndExtractDailyBomData(site, ObsCode.Daily_TempMin);
        await DownloadAndExtractDailyBomData(site, ObsCode.Daily_Rainfall);
    }
}


async Task DownloadAndExtractDailyBomData(string station, ObsCode obsCode)
{
    var dataFile = $"{station}_{obsCode.ToString().ToLower()}";
    var zipfileName = dataFile + ".zip";
    var csvFilePathAndName = @$"{obsCode.ToString().ToLower()}\{dataFile}.csv";

    // If we've already downloaded and extracted the csv, let's not do it again.
    // Prevents hammering the BOM when we already have the data.
    if (File.Exists(csvFilePathAndName))
    {
        return;
    }

    var regEx = new Regex(@"\d{6}\|\|,(?<startYear>\d{4}):(?<p_c>-?\d+),");
    var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
    var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
    using (var httpClient = new HttpClient())
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

        // This section is required to find the arcane p_c value, needed for the querystring of the dailyZippedDataFile request, that returns the daily data as a zip file
        var availableYearsUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/wData/wdata?p_stn_num={station}&p_display_type=availableYears&p_nccObsCode={(int)obsCode}";
        var response = await httpClient.GetAsync(availableYearsUrl);

        var responseContent = await response.Content.ReadAsStringAsync();
        var match = regEx.Match(responseContent);
        var p_c = match.Groups["p_c"].Value;
        var startYear = match.Groups["startYear"].Value;

        // The response of this request is a zip file that needs to be downloaded, extracted and named in a form that we'll be able to find it again by station number
        var zipFileUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_display_type=dailyZippedDataFile&p_stn_num={station}&p_nccObsCode={(int)obsCode}&p_c={p_c}&p_startYear={startYear}";
        var zipFileResponse = await httpClient.GetAsync(zipFileUrl);
        using (var fs = new FileStream(zipfileName, FileMode.OpenOrCreate))
        {
            await zipFileResponse.Content.CopyToAsync(fs);
        }

        var tempDirectory = new DirectoryInfo("temp");
        DeleteDirectory(tempDirectory);
        try
        {
            // Extract the zip file with the daily data to a folder called temp
            ZipFile.ExtractToDirectory(zipfileName, "temp");

            // Find the csv file with the data and move and rename it, putting it in the output folder (named based on the observation code)
            var csv = tempDirectory.GetFiles("*.csv").Single();
            var destinationDirectory = new DirectoryInfo(obsCode.ToString().ToLower());
            if (!destinationDirectory.Exists)
            {
                destinationDirectory.Create();
            }
            csv.MoveTo(csvFilePathAndName, true);

            // Remove the temp directory
            DeleteDirectory(tempDirectory);
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"Unable to extract zip file {zipfileName}. File may be corrupt. Message: {ex.Message}");
        }
    }
}

void DeleteDirectory(DirectoryInfo directoryInfo)
{
    if (directoryInfo.Exists)
    {
        directoryInfo.GetFiles().ToList().ForEach(file => file.Delete());
        directoryInfo.Delete();
    }
}

List<DataSetDefinition> BuildDataSetDefinitions()
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
            DataResolution = DataResolution.Daily,
            MeasurementDefinitions = new List<MeasurementDefinition>
            {
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Adjusted,
                    DataType = DataType.TempMax,
                    DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                    FolderName = "adjusted",
                    SubFolderName = "daily_tmax",
                    FileNameFormat = "tmax.[station].daily.csv",
                },
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Adjusted,
                    DataType = DataType.TempMin,
                    DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                    FolderName = "adjusted",
                    SubFolderName = "daily_tmin",
                    FileNameFormat = "tmin.[station].daily.csv",
                },
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Unadjusted,
                    DataType = DataType.TempMax,
                    DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                    FolderName = "raw-data",
                    SubFolderName = "daily_tempmax",
                    FileNameFormat = "[station]_daily_tempmax.csv",
                },
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Unadjusted,
                    DataType = DataType.TempMin,
                    DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                    FolderName = "raw-data",
                    SubFolderName = "daily_tempmin",
                    FileNameFormat = "[station]_daily_tempmin.csv",
                },
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Unadjusted,
                    DataType = DataType.Rainfall,
                    DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                    FolderName = "raw-data",
                    SubFolderName = "daily_rainfall",
                    FileNameFormat = "[station]_daily_rainfall.csv",
                }
            },
            StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
            LocationInfoUrl = "http://www.bom.gov.au/climate/data/acorn-sat/stations/#/[primaryStation]",
            HasLocations = true,
        },
        new DataSetDefinition
        {
            Id = Guid.Parse("647b6a05-43e4-48e0-a43e-04ae81a74653"),
            Name = "RAIA",
            Description = "This ACORN-SAT dataset includes homogenised monthly data from the Remote Australian Islands and Antarctica network of 8 locations, which provide ground-based temperature records.",
            MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
            FolderName = "RAIA",
            DataResolution = DataResolution.Monthly,
            MeasurementDefinitions = new List<MeasurementDefinition>
            {
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Adjusted,
                    DataType = DataType.TempMax,
                    DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$",
                    FolderName = "adjusted",
                    SubFolderName = "maxT",
                    FileNameFormat = "acorn.ria.maxT.[station].monthly.txt",
                    NullValue = "99999.9",
                },
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Adjusted,
                    DataType = DataType.TempMin,
                    DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$",
                    FolderName = "adjusted",
                    SubFolderName = "minT",
                    FileNameFormat = "acorn.ria.minT.[station].monthly.txt",
                    NullValue = "99999.9",
                },
            },
            StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
            HasLocations = true,
        },
        new DataSetDefinition
        {
            Id = Guid.Parse("88e52edd-3c67-484a-b614-91070037d47a"),
            Name = "NIWA 11-stations series",
            Description = "The National Institute of Water and Atmospheric Research (NIWA) eleven-station series are New Zealand temperature trends from a set of eleven climate stations with no significant site changes since the 1930s.",
            MoreInformationUrl = "https://niwa.co.nz/our-science/climate/information-and-resources/nz-temp-record/temperature-trends-from-raw-data",
            FolderName = "NIWA",
            DataResolution = DataResolution.Daily,
            MeasurementDefinitions = new List<MeasurementDefinition>
            {
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Unadjusted,
                    DataType = DataType.TempMax,
                    DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
                    FolderName = "raw-data",
                    FileNameFormat = "[station].csv",
                    NullValue = "-",
                },
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Unadjusted,
                    DataType = DataType.TempMin,
                    DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<value>-?[\d+\.\d+]*),-?\d*,.*,D$",
                    FolderName = "raw-data",
                    FileNameFormat = "[station].csv",
                    NullValue = "-",
                },
            },
            HasLocations = true,
        },
        new DataSetDefinition
        {
            Id = Guid.Parse("ffd5f5e2-d8df-4779-a7f4-f5d148505033"),
            Name = "Multivariate ENSO index (MEI)",
            ShortName = "MEI.v2",
            Description = "The MEI combines both oceanic and atmospheric variables to form a single index assessment of ENSO. It is an Empirical Orthogonal Function (EOF) of five different variables (sea level pressure (SLP), sea surface temperature (SST), zonal and meridional components of the surface wind, and outgoing longwave radiation (OLR)) over the tropical Pacific basin (30°S-30°N and 100°E-70°W).",
            MoreInformationUrl = "https://psl.noaa.gov/enso/mei/",
            FolderName = "ENSO",
            DataResolution = DataResolution.Monthly,
            MeasurementDefinitions = new List<MeasurementDefinition>
            {
                new MeasurementDefinition
                {
                    DataAdjustment = DataAdjustment.Unadjusted,
                    DataType = DataType.Enso,
                    DataRowRegEx = @"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                    FolderName = "MEI",
                    FileNameFormat = "meiv2.data.txt",
                    NullValue = "-999.00",
                },
            },
            DataDownloadUrl = "https://psl.noaa.gov/enso/mei/data/meiv2.data",
            HasLocations = false,
        }
    };

    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText("DataSetDefinitions.json", JsonSerializer.Serialize(dataSetDefinitions, options));

    return dataSetDefinitions;
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

List<Location> BuildAcornSatLocationsFromReferenceData(Guid dataSetId)
{
    var locations = new List<Location>();

    var locationRowData = File.ReadAllLines(@"ReferenceData\ACORN-SAT\Locations.csv");
    foreach (var row in locationRowData)
    {
        var splitRow = row.Split(',');
        var location = new Location 
        { 
            Id = Guid.NewGuid(), 
            DataSetId = dataSetId,
            Name = splitRow[0] 
        };
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
        location.PrimaryStation = primarySite;
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

    return locations;
}

public enum ObsCode
{
    Daily_TempMax = 122,
    Daily_TempMin = 123,
    Daily_Rainfall = 136,
}