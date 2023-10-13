using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnm;
using Dbscan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

var serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(x => { x.SingleLine = true; x.IncludeScopes = false; })
        )
    .BuildServiceProvider();
var logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger<Program>();

const int minimumStationsInCountry = 5;

var httpClient = new HttpClient();
var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

await DownloadAndExtract.DownloadAndExtractFile(httpClient, "https://www.ncei.noaa.gov/pub/data/ghcn/v4/", "qcf", "ghcnm.tavg.latest.qcf.tar.gz", logger);
await DownloadAndExtract.DownloadAndExtractFile(httpClient, "https://www.ncei.noaa.gov/pub/data/ghcn/v4/", "qcu", "ghcnm.tavg.latest.qcu.tar.gz", logger);

const string stationsFileCsv = @"SiteMetaData\stations.csv";
const string preExistingLocationsFile = @"SiteMetaData\pre-existing-locations.json";
const string stationsFileJson = @"Output\SiteMetaData\stations.json";

var dataStations = await GetStationFromData("qcf");

logger.LogInformation($"{dataStations.Count} stations found by reading the data files");

var ghcnIdToLocationIds = await GetGhcnIdToLocationIds(dataStations);
var fullStations = await GetStations("qcf", dataStations);

logger.LogInformation($"{fullStations.Count} stations found by reading the meta-data file and combining with country and stations found via the data files");

var filteredStations = FilterStationsByRecencyAndMinimumScore(fullStations, (short)(DateTime.Now.Year - 10), IndexCalculator.MinimumNumberOfYearsToCalculateIndex);

logger.LogInformation($"{filteredStations.Count} stations remaining after filtering based on recency and longevity of the data (records no longer than {DateTime.Now.Year - 10}) and at least {IndexCalculator.MinimumNumberOfYearsToCalculateIndex} years of data");

var combinedStations = await EnsureCountryRepresentation(fullStations, filteredStations, (short)(DateTime.Now.Year - 10));

logger.LogInformation($"{combinedStations.Count} stations after adjusting for country representation (trying to having at least 5 stations per country, even if the data is poor)");

var countryDistanceOverride = new Dictionary<string, int>
{
    { "GM", 45 },
    { "US", 70 },
};
var preExistingLocations = await GetPreExistingLocations();
var preExistingStations = preExistingLocations.Select(x => new Station
{
    Id = x.Id.ToString(),
    Name = x.Name,
    CountryCode = x.CountryCode,
    Coordinates = x.Coordinates,
    FirstYear = 1500,               // Fudge the first year so pre-existing location will always be the one chosen in the cluster
    LastYear = DateTime.Today.Year,
    YearsOfMissingData = 0,
    Source = "Pre-Existing"
}).ToList();
var totalStationSet = new List<Station>();
totalStationSet.AddRange(combinedStations);
totalStationSet.AddRange(preExistingStations);

var selectedStationsPostClustering = SelectStationsByDbscanClusteringAndTakingHighestScore(totalStationSet, 75, countryDistanceOverride, 2);

logger.LogInformation($"{selectedStationsPostClustering.Count(x => x.Source != null)} stations selected by DBSCAN were already pre-existing locations. These will now be removed from final set (they were only used to prevent station overlap).");
selectedStationsPostClustering = selectedStationsPostClustering.Where(x => x.Source == null).ToList();
var selectedStations = await RemoveDuplicateLocations(selectedStationsPostClustering);
logger.LogInformation($"{selectedStations.Count} stations have been selected after adjusting for data quality, cluster using DBSCAN and removing of duplicates due to pre-existing locations");

var locations = new List<Location>();
var dataFileLocationMapping = new DataFileLocationMapping
{
    DataSetDefinitionId = Guid.Parse("1DC38F20-3606-4D90-A2A0-84F93E75C964"),
    LocationIdToDataFileMappings = new Dictionary<Guid, List<DataFileFilterAndAdjustment>>()
};

var textinfo = CultureInfo.CurrentCulture.TextInfo;

selectedStations.ForEach(x => 
    {
        var location = new Location
        {
            Id = ghcnIdToLocationIds[x.Id],
            Name = textinfo.ToTitleCase(x.Name!.Replace('_', ' ').ToLower()),
            Coordinates = x.Coordinates!.Value,
            CountryCode = x.CountryCode,
        };
        locations.Add(location);

        dataFileLocationMapping.LocationIdToDataFileMappings.Add(
            location.Id, 
            new List<DataFileFilterAndAdjustment>
            {
                new DataFileFilterAndAdjustment
                {
                    Id = x.Id
                }
            });
    });

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters =
        {
            new JsonStringEnumConverter()
        }
};
Directory.CreateDirectory(@"Output\Location");
Directory.CreateDirectory(@"Output\Station");
Directory.CreateDirectory(@"Output\DataFileLocationMapping");

var outputFileSuffix = "_ghcnm_adjusted";

File.WriteAllText($@"Output\Location\Locations{outputFileSuffix}.json", JsonSerializer.Serialize(locations, options));
File.WriteAllText($@"Output\Station\Stations{outputFileSuffix}.json", JsonSerializer.Serialize(selectedStations, options));
File.WriteAllText($@"Output\DataFileLocationMapping\DataFileLocationMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileLocationMapping, options));




await CreateStationDataFiles("qcf", selectedStations, logger);
await CreateStationDataFiles("qcu", selectedStations, logger);

List<Station> SelectStationsByDbscanClusteringAndTakingHighestScore(List<Station> processedStations, double distance, Dictionary<string, int> countryDistanceOverride, int minimumPointsPerCluster)
{
    var stationsGroupedByCountry = processedStations.GroupBy(x => x.CountryCode);

    var selectedStations = new List<Station>();
    foreach (var groupedCountry in stationsGroupedByCountry)
    {
        var stationsInCountry = groupedCountry.ToList();

        var index = new GeoListSpatialIndex<PointInfo<GeoPoint>>(stationsInCountry.Select(x => new PointInfo<GeoPoint>(new GeoPoint(x.Id, x.Coordinates!.Value.Latitude, x.Coordinates.Value.Longitude))));

        var distanceForClustering = countryDistanceOverride.ContainsKey(groupedCountry.Key!) ? countryDistanceOverride[groupedCountry.Key!] : distance;
        var dbscanResult = Dbscan.Dbscan.CalculateClusters(
                index,
                epsilon: distanceForClustering,
                minimumPointsPerCluster: minimumPointsPerCluster);

        foreach (var cluster in dbscanResult.Clusters)
        {
            int highestScore = 0;
            var selectedStationId = cluster.Objects.First().Id;
            foreach (var geoPoint in cluster.Objects)
            {
                var geoPointStation = stationsInCountry.Single(x => x.Id == geoPoint.Id);

                if (geoPointStation.Score > highestScore)
                {
                    logger.LogInformation($"Station {geoPointStation.Id} has a score of {geoPointStation.Score}, beating the current highest of {highestScore}");
                    highestScore = geoPointStation.Score.Value;
                    selectedStationId = geoPointStation.Id;
                }
                else
                {
                    logger.LogInformation($"Station {geoPointStation.Id} has a score of {geoPointStation.Score}. That is not higher than {highestScore}");
                }
            }
            logger.LogInformation($"Station {selectedStationId} has been selected");
            var selectedStation = stationsInCountry.Single(x => x.Id == selectedStationId);
            selectedStations.Add(selectedStation);
        }

        foreach (var unclusteredStation in dbscanResult.UnclusteredObjects)
        {
            var station = stationsInCountry.Single(x => x.Id == unclusteredStation.Id);
            logger.LogInformation($"Unclustered station {unclusteredStation.Id} has been selected");
            selectedStations.Add(station);
        }
    }

    return selectedStations;
}

async Task SaveStationMetaData(List<Station> stations)
{
    var contents = stations.Select(x => $"{x.Id},{x.FirstYear!.Value},{x.LastYear!.Value},{x.YearsOfMissingData ?? 0}");
    await File.WriteAllLinesAsync(stationsFileCsv, contents);
}

async Task<List<Station>> GetStationFromData(string version)
{
    List<Station> stations;
    if (File.Exists(stationsFileCsv))
    {
        stations = await GetPreProcessedStations();
        return stations;
    }

    var dir = new DirectoryInfo(@$"SourceData\{version}\");
    var dataFileName = dir.GetFiles("*.dat").Single().FullName;

    var records = File.ReadAllLines(dataFileName);
    stations = new List<Station>();

    foreach (var record in records)
    {
        // Sample line of data
        // ACW000116041993TAVG-9999     127  }-9999   -9999   -9999   -9999   -9999   -9999    1067  }  757  }  267  }  167  }

        var id = record[..11];
        var year = int.Parse(record.Substring(11, 4));

        var validYear = IsValidYear(record);
        if (!validYear)
        {
            logger.LogInformation($"{year} for station {id} has a month without data. {year} is not considered to be a valid year");
        }

        var station = stations.SingleOrDefault(s => s.Id == id);
        // If we don't have a record for the station yet, don't create it if we don't have a valid year of data
        if (station == null && !validYear)
        {
            continue;
        }

        if (station == null)
        {
            station = new Station
            {
                Id = id,
                FirstYear = year,
                LastYear = year,
            };
            stations.Add(station);
        }

        if (!validYear)
        {
            continue;
        }
        else if (year < station.LastYear!.Value)
        {
            throw new Exception($"Record year ({year}) is less than the end year for the station {station.Id}");
        }
        else
        {
            var yearsOfMissingData = year - station.LastYear.Value - 1;
            if (yearsOfMissingData < -1)
            {
                throw new Exception("Invalid data record ordering");
            }
            if (yearsOfMissingData > 0)
            {
                station.YearsOfMissingData = station.YearsOfMissingData == null ? yearsOfMissingData : station.YearsOfMissingData + yearsOfMissingData;
            }
            station.LastYear = year;
        }
    }

    await SaveStationMetaData(stations);

    return await GetStationFromData(version);
}

static bool IsValidYear(string record)
{
    var validYear = true;

    for (var i = 0; i < 12; i++)
    {
        var value = record.Substring(19 + (i * 8), 5);
        if (value == "-9999")
        {
            validYear = false;
            break;
        }
    }

    return validYear;
}

async Task<List<Station>> GetStations(string version, List<Station> inputStations)
{
    if (File.Exists(stationsFileJson))
    {
        return GetStationsFromFile();
    }
    
    var countries = await Country.GetCountries(@"SiteMetaData\ghcnm-countries.txt");
    var stations = await StationFile.Load(version, inputStations, countries, logger);

    SaveStations(stations, stationsFileJson);

    return GetStationsFromFile();
}

List<Station> FilterStationsByRecencyAndMinimumScore(List<Station> stations, short lastYearOfDataNoLaterThan, short minimumScore)
{
    var selectedStations = new List<Station>();
    foreach (var station in stations)
    {
        if (station.LastYear < lastYearOfDataNoLaterThan)
        {
            logger!.LogInformation($"Station {station.Id} is being filtered out because it isn't contemporary. Last record was in {station.LastYear.Value}");
            continue;
        }
        if (station.Score < minimumScore)
        {
            logger!.LogInformation($"Station {station.Id} is being filtered out because it has too much missing data. It's score ({station.Score}) (i.e., age ({station.Age}) - number of years of missing data ({station.YearsOfMissingData})) is less than the minimum score ({minimumScore})");
            continue;
        }
        logger!.LogInformation($"Station {station.Id} has been accepted. Its last year of records was {station.LastYear!.Value} and it's age is {station.Age} and its score is {station.Score}");
        selectedStations.Add(station);
    }
    return selectedStations;
}

List<Station> GetStationsFromFile()
{
    if (!File.Exists(stationsFileJson))
    {
        throw new FileNotFoundException($"File {stationsFileJson} does not exist");
    }

    var contents = File.ReadAllText(stationsFileJson);
    var stations = JsonSerializer.Deserialize<List<Station>>(contents);

    return stations!;
}

void SaveStations(List<Station> stations, string path)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
    };
    var contents = JsonSerializer.Serialize(stations, options);
    File.WriteAllText(path, contents);
}

async static Task<List<Station>> GetPreProcessedStations()
{
    var stations = new List<Station>();
    var contents = await File.ReadAllLinesAsync(stationsFileCsv);

    foreach (var line in contents)
    {
        var columns = line.Split(',');
        var station = new Station
        {
            Id = columns[0],
            FirstYear = int.Parse(columns[1]),
            LastYear = int.Parse(columns[2]),
            YearsOfMissingData = int.Parse(columns[3]),
        };
        stations.Add(station);
    }
    return stations;
}

static async Task CreateStationDataFiles(string version, List<Station> stations, ILogger<Program> logger)
{
    var outputFolder = @$"Output\Data\{version}\";
    var dir = new DirectoryInfo(outputFolder);
    if (dir.Exists)
    {
        dir.Delete(true);
    }
    dir.Create();

    var sourceDir = new DirectoryInfo(@$"SourceData\{version}\");
    var dataFileName = sourceDir.GetFiles("*.dat").Single().FullName;

    var records = await File.ReadAllLinesAsync(dataFileName);

    string? currentStation = null;
    StreamWriter? writer = null;

    foreach (var record in records)
    {
        var id = record.Substring(0, 11);

        var station = stations.SingleOrDefault(s => s.Id == id);
        if (station == null)
        {
            continue;
        }

        var year = int.Parse(record.Substring(11, 4));
        var validYear = IsValidYear(record);

        if (validYear)
        {
            if (currentStation != id)
            {
                if (writer != null)
                {
                    writer.Close();
                }
                currentStation = id;
                var fileName = $@"{outputFolder}{id}.csv";
                if (File.Exists(fileName))
                {
                    throw new Exception($"File {fileName} exists already");
                }
                logger.LogInformation($"Creating data file for station {id}");
                writer = File.CreateText(fileName);
            }

            var values = GetValues(record);

            writer!.WriteLine($"{year},{string.Join(',', values)}");
        }
    }

    writer!.Close();
}

static int[] GetValues(string record)
{
    var values = new int[12];
    for (var i = 0; i < 12; i++)
    {
        var value = record.Substring(19 + (i * 8), 5).Trim();
        values[i] = int.Parse(value);
    }
    return values;
}

static async Task<Dictionary<string, Guid>> GetGhcnIdToLocationIds(List<Station> stations)
{
    const string ghcnIdToLocationIdsFile = @"SiteMetaData\GhcnIdToLocationIds.json";
    Dictionary<string, Guid>? ghcnIdToLocationIds = null;
    if (File.Exists(ghcnIdToLocationIdsFile))
    {
        var contents = await File.ReadAllTextAsync(ghcnIdToLocationIdsFile);
        ghcnIdToLocationIds = JsonSerializer.Deserialize<Dictionary<string, Guid>>(contents);
    }
    else
    {
        ghcnIdToLocationIds = stations.ToDictionary<Station, string, Guid>(x => x.Id, x => Guid.NewGuid());
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        var contents = JsonSerializer.Serialize(ghcnIdToLocationIds, jsonOptions);
        await File.WriteAllTextAsync(ghcnIdToLocationIdsFile, contents);
        return await GetGhcnIdToLocationIds(stations);
    }

    return ghcnIdToLocationIds!;
}

async Task<List<Station>> RemoveDuplicateLocations(List<Station> dataQualityFilteredStations)
{
    var preExistingLocations = await GetPreExistingLocations();
    var stationsRemoved = 0;
    foreach (var preExistingLocation in preExistingLocations)
    {
        var stationMatchedOnName = dataQualityFilteredStations.SingleOrDefault(x => x.Name!.Replace('_', ' ').Equals(preExistingLocation.Name, StringComparison.OrdinalIgnoreCase));
        if (stationMatchedOnName != null)
        {
            logger.LogInformation($"Found match of station based on name ({stationMatchedOnName!.Name}). Will remove this station from the set");
            dataQualityFilteredStations.Remove(stationMatchedOnName);
            stationsRemoved++;
        }
        else
        {
            stationMatchedOnName = dataQualityFilteredStations.SingleOrDefault(x => MathF.Round(x.Coordinates!.Value.Latitude, 1) == MathF.Round(preExistingLocation.Coordinates.Latitude, 1)
                                                                                 && MathF.Round(x.Coordinates.Value.Longitude, 1) == MathF.Round(preExistingLocation.Coordinates.Longitude, 1));
            if (stationMatchedOnName != null)
            {
                logger.LogInformation($"Found match of station based on geo-coordinates (their names are '{stationMatchedOnName!.Name}' and '{preExistingLocation.Name}'). Will remove this station from the set");
                dataQualityFilteredStations.Remove(stationMatchedOnName);
                stationsRemoved++;
            }
        }
    }

    logger.LogInformation($"{stationsRemoved} duplicate stations removed");

    return dataQualityFilteredStations;
}

async Task<List<Location>> GetPreExistingLocations()
{
    var contents = await File.ReadAllTextAsync(preExistingLocationsFile);
    var preExistingLocations = JsonSerializer.Deserialize<List<Location>>(contents)!;
    return preExistingLocations;
}

async Task<List<Station>> EnsureCountryRepresentation(List<Station> stations, List<Station> selectedStations, short lastYearOfDataNoLaterThan)
{
    var countries = await Country.GetCountries(@"SiteMetaData\ghcnm-countries.txt");
    var combinedStations = new List<Station>();
    combinedStations.AddRange(selectedStations);
    foreach (var country in countries)
    {
        var stationCount = selectedStations
            .Count(x => x.CountryCode == country.Key);

        if (stationCount < minimumStationsInCountry)
        {
            var countryCount = stations
                .Count(x => x.CountryCode == country.Key);

            if (countryCount == 0)
            {
                logger.LogInformation($"{country.Value} has {stationCount} selected stations out of {countryCount} in GHCNm. Will skip this country");
                continue;
            }

            logger.LogInformation($"{country.Value} has {stationCount} selected stations out of {countryCount} in GHCNm. Will try to add more stations to bring the number to a minimum of {minimumStationsInCountry}");
            var bestStations = stations
                .Where(x => x.CountryCode == country.Key 
                            && x.Score >= 10
                            && x.LastYear >= lastYearOfDataNoLaterThan)
                .OrderByDescending(x => x.Score).Take(minimumStationsInCountry);
            foreach (var goodStation in bestStations)
            {
                if (!selectedStations.Any(x => x.Id == goodStation.Id))
                {
                    logger.LogInformation($"Adding station {goodStation.Id} to the collection increase representation");
                    combinedStations.Add(goodStation);
                }
            }
        }
    }
    return combinedStations;
}