using ClimateExplorer.Data.Ghcnm;
using Dbscan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;

var serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(x => { x.SingleLine = true; x.IncludeScopes = false; })
        )
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

var stations = await GetStationMetaData();

var processedStations = await RetrieveDataProcessedStations(stations);

//var selectedStations = SelectStationsSinglePerCountry(processedStations);

//CalculateDistances(processedStations);

//var farStations = SelectStationsMinimumDistanceFromNeighbour(processedStations, 100);

//foreach (var farStation in farStations)
//{
//    if (!selectedStations.Any(x => x.Id == farStation.Id))
//    {
//        selectedStations.Add(farStation);
//    }
//}

var selectedStations = SelectStationsByClusteringAndTakingBest(processedStations, 100, 2);

SaveStations(selectedStations, @"Output\SiteMetaData\selected-stations.json");

await CreateStationDataFiles(selectedStations);

List<Station> SelectStationsByClusteringAndTakingBest(List<Station> processedStations, double distance, int minimumPointsPerCluster)
{
    var stationsGroupedByCountry = processedStations.GroupBy(x => x.CountryCode);

    var selectedStations = new List<Station>();
    foreach (var groupedCountry in stationsGroupedByCountry)
    {
        var stationsInCountry = groupedCountry.ToList();

        var index = new GeoListSpatialIndex<PointInfo<GeoPoint>>(stationsInCountry.Select(x => new PointInfo<GeoPoint>(new GeoPoint(x.Id, x.Coordinates.Latitude, x.Coordinates.Longitude))));

        var result = Dbscan.Dbscan.CalculateClusters(
                index,
                epsilon: distance,
                minimumPointsPerCluster: minimumPointsPerCluster);

        foreach (var cluster in result.Clusters)
        {
            int bestScore = 0;
            var selectedStationId = cluster.Objects.First().Id;
            foreach (var geoPoint in cluster.Objects)
            {
                var geoPointStation = stationsInCountry.Single(x => x.Id == geoPoint.Id);

                var score = geoPointStation.Age - geoPointStation.YearsOfMissingData;

                if (score > bestScore)
                {
                    logger.LogInformation($"Station {geoPointStation.Id} has a score of {score}, beating the current best of {bestScore}");
                    bestScore = score;
                    selectedStationId = geoPointStation.Id;
                }
            }
            logger.LogInformation($"Station {selectedStationId} has been selected");
            var selectedStation = stationsInCountry.Single(x => x.Id == selectedStationId);
            selectedStations.Add(selectedStation);
        }

        foreach (var unclusteredStation in result.UnclusteredObjects)
        {
            var station = stationsInCountry.Single(x => x.Id == unclusteredStation.Id);
            selectedStations.Add(station);
        }
    }

    return selectedStations;
}

void SaveStationMetaData(List<Station> stations)
{
    var contents = stations.Select(x => $"{x.Id},{x.Begin.Year},{x.End.Year},{x.YearsOfMissingData}");

    File.WriteAllLines(@"SiteMetaData\stations.csv", contents);
}

async Task<List<Station>> GetStationMetaData()
{
    if (File.Exists(@"SiteMetaData\stations.csv"))
    {
        var stations = GetPreProcessedStations();
        return stations;
    }

    var records = File.ReadAllLines(@"data\ghcnm.tavg.v4.0.1.20230817.qcf.dat");
    stations = new List<Station>();

    foreach (var record in records)
    {
        // Sample line of data
        // ACW000116041993TAVG-9999     127  }-9999   -9999   -9999   -9999   -9999   -9999    1067  }  757  }  267  }  167  }

        var id = record.Substring(0, 11);
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
                Begin = new DateOnly(year, 1, 1),
                End = new DateOnly(year, 12, 31),
            };
            stations.Add(station);
        }

        if (!validYear)
        {
            continue;
        }
        else if (year < station.End.Year)
        {
            throw new Exception($"Record year ({year}) is less than the end year for the station {station.Id}");
        }
        else
        {
            var yearsOfMissingData = year - station.End.Year - 1;
            if (yearsOfMissingData < -1)
            {
                throw new Exception("Invalid data record ordering");
            }
            if (yearsOfMissingData > 0)
            {
                station.YearsOfMissingData += yearsOfMissingData;
            }
            station.End = new DateOnly(year, 12, 31);
        }
    }

    SaveStationMetaData(stations);

    return stations;
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

const string dataFilteredStations = @"Output\SiteMetaData\data-filtered-stations.json";

async Task<List<Station>> RetrieveDataProcessedStations(List<Station> inputStations)
{
    if (File.Exists(dataFilteredStations))
    {
        return GetProcessedStations();
    }
    
    var countries = await CountryFileProcessor.Transform();
    var stations = await StationFileProcessor.Transform(inputStations, countries, 1970, (short)(DateTime.Now.Year - 10), .5f, logger);

    SaveStations(stations, dataFilteredStations);

    return GetProcessedStations();
}

List<Station> GetProcessedStations()
{
    if (!File.Exists(dataFilteredStations))
    {
        throw new FileNotFoundException($"File {dataFilteredStations} does not exist");
    }

    var contents = File.ReadAllText(dataFilteredStations);
    var stations = JsonSerializer.Deserialize<List<Station>>(contents);

    return stations;
}

void SaveStations(List<Station> stations, string path)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path));

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
    };
    var contents = JsonSerializer.Serialize(stations, options);
    File.WriteAllText(path, contents);
}

static List<Station> GetPreProcessedStations()
{
    var stations = new List<Station>();
    var contents = File.ReadAllLines(@"SiteMetaData\stations.csv");

    foreach (var line in contents)
    {
        var columns = line.Split(',');
        var station = new Station()
        {
            Id = columns[0],
            Begin = new DateOnly(int.Parse(columns[1]), 1, 1),
            End = new DateOnly(int.Parse(columns[2]), 12, 31),
            YearsOfMissingData = int.Parse(columns[3]),
        };
        stations.Add(station);
    }
    return stations;
}

static async Task CreateStationDataFiles(List<Station> stations)
{
    var dir = new DirectoryInfo(@"Output\Data\");
    if (dir.Exists)
    {
        dir.Delete(true);
    }
    dir.Create();

    var records = await File.ReadAllLinesAsync(@"data\ghcnm.tavg.v4.0.1.20230817.qcf.dat");

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
                var fileName = $@"Output\Data\{id}.csv";
                if (File.Exists(fileName))
                {
                    throw new Exception($"File {fileName} exists already");
                }
                writer = File.CreateText(fileName);
            }

            var values = GetValues(record);

            writer.WriteLine($"{year},{string.Join(',', values)}");
        }
    }

    writer.Close();
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

public class GeoPoint : IPointData
{
    public GeoPoint(string id, double x, double y)
    {
        Id = id;
        Point = new Point(x, y);
    }

    public string Id { get; }
    public Point Point { get; }
}