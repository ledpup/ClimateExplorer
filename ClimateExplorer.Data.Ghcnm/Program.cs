using ClimateExplorer.Core;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnm;
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

// A station needs this many complete years in the last 30, so its "last 30 years" trend (which takes the last 30
// complete years) covers roughly the last 30 calendar years. 25 is too strict: one missing month voids a year.
const int recentPeriodYears = 30;
const int minimumCompleteYearsInRecentPeriod = 20;

var httpClient = new HttpClient();
var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

await DownloadAndExtract.DownloadAndExtractFile(httpClient, "https://www.ncei.noaa.gov/pub/data/ghcn/v4/", "qcf", "ghcnm.tavg.latest.qcf.tar.gz", logger);
await DownloadAndExtract.DownloadAndExtractFile(httpClient, "https://www.ncei.noaa.gov/pub/data/ghcn/v4/", "qcu", "ghcnm.tavg.latest.qcu.tar.gz", logger);

const string stationsFileCsv = @"MetaData\stations.csv";
const string preExistingLocationsFile = @"MetaData\pre-existing-locations.json";
// Holds ISO country codes. Renamed from stations.json so a cache from before the FIPS to ISO mapping is never read.
const string stationsFileJson = @"Output\MetaData\stations-iso.json";

var dataStations = await GetStationFromData("qcf");

logger.LogInformation($"{dataStations.Count} stations found by reading the data files");

var ghcnIdToLocationIds = await GetGhcnIdToLocationIds(dataStations, logger);
var fullStations = await GetStations("qcf", dataStations);

logger.LogInformation($"{fullStations.Count} stations found by reading the meta-data file and combining with country and stations found via the data files");

var newStations = new List<string>();
fullStations.ForEach(x =>
{
    if (!ghcnIdToLocationIds.ContainsKey(x.Id))
    {
        // A renumbered station keeps the location ID it had under its old station ID, so links to it still work.
        var previousId = GhcnmStationIds.GetPreviousId(x.Id);
        var locationId = previousId != null && ghcnIdToLocationIds.TryGetValue(previousId, out var previousLocationId)
            ? previousLocationId
            : Guid.NewGuid();
        ghcnIdToLocationIds.Add(x.Id, locationId);
        newStations.Add(x.Id);
    }
});
if (newStations.Any())
{
    logger.LogInformation($"There are new stations that are not found in ghcnIdToLocationIds. These stations are: {string.Join(", ", newStations)}");
    logger.LogWarning($"Will now save an updated ghcnIdToLocationIds file");
    await SaveGhcnIdToLocationIds(ghcnIdToLocationIds!);
    logger.LogWarning($"PLEASE RE-RUN THIS APP");
    Environment.Exit(0);
}

var completeYearsInRecentPeriod = await GetCompleteYearsInRecentPeriod("qcf", DateTime.Now.Year - recentPeriodYears, DateTime.Now.Year - 1);
var filteredStations = FilterStationsByRecencyAndMinimumScore(fullStations, (short)(DateTime.Now.Year - 10), AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly, completeYearsInRecentPeriod);

logger.LogInformation($"{filteredStations.Count} stations remaining after filtering based on recency and longevity of the data (records no longer than {DateTime.Now.Year - 10}), at least {AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly} complete years of data, and at least {minimumCompleteYearsInRecentPeriod} complete years in the last {recentPeriodYears}");

var combinedStations = await EnsureCountryRepresentation(fullStations, filteredStations, (short)(DateTime.Now.Year - 10));

logger.LogInformation($"{combinedStations.Count} stations after adjusting for country representation (trying to having at least 5 stations per country, even if the data is poor)");

// Locations from other sources (BOM, UK). Stations near them are left out, as they'd duplicate them.
var preExistingLocations = await GetPreExistingLocations();
var preExistingStations = preExistingLocations.Select(x => new Station
{
    Id = x.Id.ToString(),
    Name = x.Name,
    CountryCode = x.CountryCode,
    Coordinates = x.Coordinates,
    Source = "Pre-Existing"
}).ToList();

// The stations selected last time go first, so a re-run keeps them (and their URLs) unless they now fail the filters.
// The rest go best record first.
var previouslySelectedIds = await GetPreviouslySelectedStationIds();
var candidates = combinedStations
    .OrderByDescending(x => previouslySelectedIds.Contains(x.Id) || previouslySelectedIds.Contains(GhcnmStationIds.GetPreviousId(x.Id) ?? string.Empty))
    .ThenByDescending(x => x.Score ?? 0)
    .ThenBy(x => x.Id)
    .ToList();

// Spacing is per country: people look for a station in their own country, even when a better one is just across
// the border. The map hides markers that would overlap until the user zooms in.
var spacedStations = StationSpacingSelector.Select(
    preExistingStations,
    candidates,
    x => x.CountryCode!,
    x => x.Coordinates!.Value,
    x => GetSpacingRadiusKm(x.Score ?? 0));

logger.LogInformation($"{spacedStations.Count} of {candidates.Count} stations kept after leaving out those close to a better station in the same country or to a pre-existing location");

var selectedStations = await RemoveDuplicateLocations(spacedStations);
logger.LogInformation($"{selectedStations.Count} stations have been selected after adjusting for data quality, spacing and removing duplicates of pre-existing locations");

var locations = new List<Location>();
var dataFileMapping = new DataFileMapping
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
            CountryCode = x.CountryCode!,
        };
        locations.Add(location);

        dataFileMapping.LocationIdToDataFileMappings.Add(
            location.Id,
            [
                new()
                {
                    Id = x.Id
                }
            ]);
    });

var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

var outputFileSuffix = "_ghcnm_adjusted";

File.WriteAllText(Folders.MetaDataFolder + $@"Location\Locations{outputFileSuffix}.json", JsonSerializer.Serialize(locations, jsonSerializerOptions));
File.WriteAllText(Folders.MetaDataFolder + $@"Station\Stations{outputFileSuffix}.json", JsonSerializer.Serialize(selectedStations, jsonSerializerOptions));
File.WriteAllText(Folders.MetaDataFolder + $@"DataFileMapping\DataFileMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileMapping, jsonSerializerOptions));


var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();
var ghcnm = dataSetDefinitions.Single(x => x.ShortName == "GHCNm");

await CreateStationDataFiles("qcf", @"Temperature\GHCNm\Adjusted", selectedStations, logger);
await CreateStationDataFiles("qcu", @"Temperature\GHCNm\Unadjusted", selectedStations, logger);

// The map hides markers that would overlap, so spacing only removes near-duplicates (an airport and its town,
// several stations in one city). 25 km for a station with the minimum record, shrinking to 10 km at 120 years.
static double GetSpacingRadiusKm(int completeYears)
{
    const double shortRecordRadiusKm = 25;
    const double longRecordRadiusKm = 10;
    const int shortRecordYears = AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly;
    const int longRecordYears = 120;

    var fraction = Math.Clamp((completeYears - shortRecordYears) / (double)(longRecordYears - shortRecordYears), 0, 1);
    return shortRecordRadiusKm - ((shortRecordRadiusKm - longRecordRadiusKm) * fraction);
}

// The stations the last run selected, from the station file it wrote for the WebApi.
static async Task<HashSet<string>> GetPreviouslySelectedStationIds()
{
    if (!File.Exists(Folders.SelectedStationsFile))
    {
        return [];
    }

    var stations = await Station.GetStationsFromFile(Folders.SelectedStationsFile);
    return [.. stations.Select(x => x.Id)];
}

// Counts each station's complete years (all 12 months present) from firstYear to lastYear.
static async Task<Dictionary<string, int>> GetCompleteYearsInRecentPeriod(string version, int firstYear, int lastYear)
{
    var dir = new DirectoryInfo(@$"SourceData\{version}\");
    var dataFileName = dir.GetFiles("*.dat").Single().FullName;

    var counts = new Dictionary<string, int>();
    foreach (var record in await File.ReadAllLinesAsync(dataFileName))
    {
        var year = int.Parse(record.Substring(11, 4));
        if (year < firstYear || year > lastYear || !IsValidYear(record))
        {
            continue;
        }

        var id = record[..11];
        counts[id] = counts.GetValueOrDefault(id) + 1;
    }

    return counts;
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
    stations = [];

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

    var fipsToIsoMapper = await FipsToIsoMapper.Load();
    var stations = await StationFile.Load(version, inputStations, fipsToIsoMapper, logger);

    SaveStations(stations, stationsFileJson);

    return GetStationsFromFile();
}

List<Station> FilterStationsByRecencyAndMinimumScore(List<Station> stations, short lastYearOfDataNoLaterThan, short minimumScore, Dictionary<string, int> completeYearsInRecentPeriod)
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
        var recentCompleteYears = completeYearsInRecentPeriod.GetValueOrDefault(station.Id);
        if (recentCompleteYears < minimumCompleteYearsInRecentPeriod)
        {
            logger!.LogInformation($"Station {station.Id} is being filtered out because only {recentCompleteYears} of the last {recentPeriodYears} years are complete (minimum {minimumCompleteYearsInRecentPeriod})");
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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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

static async Task CreateStationDataFiles(string version, string outputFolderName, List<Station> stations, ILogger<Program> logger)
{
    var outputFolder = Folders.SourceDataFolder + outputFolderName;
    var dir = new DirectoryInfo(outputFolder);
    if (dir.Exists)
    {
        dir.Delete(true);
    }
    dir.Create();

    var sourceDir = new DirectoryInfo(@$"SourceData\{version}\");
    var dataFileName = sourceDir.GetFiles("*.dat").Single().FullName;

    var records = await File.ReadAllLinesAsync(dataFileName);
    var stationIds = stations.Select(x => x.Id).ToHashSet();

    string? currentStation = null;
    StreamWriter? writer = null;

    foreach (var record in records)
    {
        var id = record.Substring(0, 11);

        if (!stationIds.Contains(id))
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
                var fileName = $@"{outputFolder}\{id}.csv";
                if (File.Exists(fileName))
                {
                    logger.LogInformation($"Opening data file for station {id}");
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

static async Task<Dictionary<string, Guid>> GetGhcnIdToLocationIds(List<Station> stations, ILogger logger)
{
    const string ghcnIdToLocationIdsFile = @"MetaData\GhcnIdToLocationIds.json";
    Dictionary<string, Guid>? ghcnIdToLocationIds = null;
    if (File.Exists(ghcnIdToLocationIdsFile))
    {
        var contents = await File.ReadAllTextAsync(ghcnIdToLocationIdsFile);
        ghcnIdToLocationIds = JsonSerializer.Deserialize<Dictionary<string, Guid>>(contents);
        return ghcnIdToLocationIds!;
    }

    throw new FileNotFoundException($"{ghcnIdToLocationIdsFile} not found");
}

static async Task SaveGhcnIdToLocationIds(Dictionary<string, Guid>? ghcnIdToLocationIds)
{
    const string ghcnIdToLocationIdsFile = @"..\..\..\MetaData\GhcnIdToLocationIds.json";

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    var contents = JsonSerializer.Serialize(ghcnIdToLocationIds, jsonOptions);
    await File.WriteAllTextAsync(ghcnIdToLocationIdsFile, contents);
}

async Task<List<Station>> RemoveDuplicateLocations(List<Station> dataQualityFilteredStations)
{
    var preExistingLocations = await GetPreExistingLocations();
    var stationsRemoved = 0;
    foreach (var preExistingLocation in preExistingLocations)
    {
        // Same country only: a pre-existing Perth (Australia) mustn't remove Perth in Scotland.
        // With more stations selected, more than one can match, so remove them all.
        var stationsInCountry = dataQualityFilteredStations.Where(x => x.CountryCode == preExistingLocation.CountryCode).ToList();
        var matchedOn = "name";
        var matches = stationsInCountry
            .Where(x => x.Name!.Replace('_', ' ').Equals(preExistingLocation.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            matchedOn = "geo-coordinates";
            matches = stationsInCountry
                .Where(x => Math.Round(x.Coordinates!.Value.Latitude, 1) == Math.Round(preExistingLocation.Coordinates.Latitude, 1)
                         && Math.Round(x.Coordinates.Value.Longitude, 1) == Math.Round(preExistingLocation.Coordinates.Longitude, 1))
                .ToList();
        }

        foreach (var match in matches)
        {
            logger.LogInformation($"Found match of station based on {matchedOn} (their names are '{match.Name}' and '{preExistingLocation.Name}'). Will remove this station from the set");
            dataQualityFilteredStations.Remove(match);
            stationsRemoved++;
        }
    }

    logger.LogInformation($"{stationsRemoved} duplicate stations removed");

    return dataQualityFilteredStations;
}

async Task<List<Location>> GetPreExistingLocations()
{
    var contents = await File.ReadAllTextAsync(preExistingLocationsFile);
    var preExistingLocations = JsonSerializer.Deserialize<List<Location>>(contents, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    return preExistingLocations;
}

async Task<List<Station>> EnsureCountryRepresentation(List<Station> stations, List<Station> selectedStations, short lastYearOfDataNoLaterThan)
{
    var fipsToIsoMapper = await FipsToIsoMapper.Load();
    var combinedStations = new List<Station>();
    combinedStations.AddRange(selectedStations);
    foreach (var countryCode in fipsToIsoMapper.IsoCodes)
    {
        var stationCount = selectedStations
            .Count(x => x.CountryCode == countryCode);

        if (stationCount < minimumStationsInCountry)
        {
            var countryCount = stations
                .Count(x => x.CountryCode == countryCode);

            if (countryCount == 0)
            {
                logger.LogInformation($"{countryCode} has {stationCount} selected stations out of {countryCount} in GHCNm. Will skip this country");
                continue;
            }

            logger.LogInformation($"{countryCode} has {stationCount} selected stations out of {countryCount} in GHCNm. Will try to add more stations to bring the number to a minimum of {minimumStationsInCountry}");
            var bestStations = stations
                .Where(x => x.CountryCode == countryCode
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
