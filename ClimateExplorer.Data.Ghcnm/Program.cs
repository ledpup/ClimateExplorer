using ClimateExplorer.Data.Ghcnm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

var serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(x => { x.SingleLine = true; x.IncludeScopes = false; })
        )
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

var stations = await GetStationMetaData();

var filteredStations = await RetrieveFilteredStations(stations);

foreach (var station in filteredStations)//.Where(x => x.Name.Contains("LAUNCESTON")))
{
    //logger.BeginScope(station.Name);

    //if (File.Exists($@"Output\ProcessedRecords\{station.FileName}.csv"))
    //{
    //    logger.LogInformation($"Station file {station.FileName}.csv already exists. Download and processing of ISD file will not be done.");
    //    continue;
    //}

    //var results = await GetStationRecords(httpClient, station, logger);
    //if (results.MissingYears.Any())
    //{
    //    logger.LogWarning($"There are {results.MissingYears.Count} missing years for {station.FileName} where data is expected from {station.Begin.Year} to {station.End.Year}. They are: { string.Join(',', results.MissingYears) }");
    //}
    //DataRecordFileSaver.Save(station.FileName, results.Records, logger);
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

        var validYear = true;

        for (var i = 0; i < 12; i++)
        {
            var value = record.Substring(19 + (i * 8), 5);
            if (value == "-9999")
            {
                validYear = false;
                logger.LogInformation($"{year} for station {id} has a month ({GetFullMonthName(i + 1)}) without data. {year} is not considered to be a valid year");
                break;
            }
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

static string GetFullMonthName(int month)
{
    DateTime date = new DateTime(2020, month, 1);

    return date.ToString("MMMM");
}

async Task<List<Station>> RetrieveFilteredStations(List<Station> inputStations)
{
    var countries = await CountryFileProcessor.Transform();

    var stations = await StationFileProcessor.Transform(inputStations, countries, 1950, (short)(DateTime.Now.Year - 10), .5f, logger);
    var stationsGroupedByCountry = stations.Values.GroupBy(x => x.Country);

    var messages = new List<string>();

    foreach (var groupedStation in stationsGroupedByCountry.OrderByDescending(x => x.ToList().Count))
    {
        var stationList = groupedStation.ToList();

        messages.Add($"{(groupedStation.Key == null ? "No country" : groupedStation.Key.Name)} {stationList.Count} average age {Math.Round(groupedStation.Average(x => x.Age), 0)} years");

        if (stationList.Count > 1)
        {
            foreach (var station in stationList)
            {
                station.StationDistances = StationDistance.GetDistances(station, stationList);
                station.AverageDistance = station.StationDistances.Average(x => x.Distance);
            }
        }
        else
        {
            stationList.ForEach(x => x.AverageDistance = 1000 * 1000);
        }

        stationList.ForEach(x => x.Score = (x.Age / 10) * (x.AverageDistance / 1000D) * (1D / stationList.Count));
        stationList = stationList.OrderByDescending(x => x.Score).ToList();

        stationList.ForEach(x => messages.Add($"    {x.Id} score {x.Score}"));
    }


    messages.Add($"{stations.Count} stations from {stationsGroupedByCountry.Count()} countries");

    Directory.CreateDirectory("Output");
    File.WriteAllLines(@"Output\stations - unfiltered.txt", messages);

    var filteredStations = stations.Where(x => x.Value.Score >= 70).ToDictionary(x => x.Key, x => x.Value);

    messages = new List<string>();

    stationsGroupedByCountry = filteredStations.Values.GroupBy(x => x.Country);
    foreach (var groupedStation in stationsGroupedByCountry.OrderByDescending(x => x.ToList().Count))
    {
        var stationList = groupedStation.ToList();
        messages.Add($"{(groupedStation.Key == null ? "No country" : groupedStation.Key.Name)} {stationList.Count} average age {Math.Round(groupedStation.Average(x => x.Age), 0)} years");
    }

    messages.Add($"{filteredStations.Count} stations from {stationsGroupedByCountry.Count()} countries");

    File.WriteAllLines(@"Output\stations - filtered.txt", messages);

    return filteredStations.Values.ToList();
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

//static async Task<(Dictionary<DateOnly, List<TimedRecord>> Records, List<int> MissingYears)> GetStationRecords(HttpClient httpClient, Station station, ILogger<Program> logger)
//{
//    var dataRecords = new Dictionary<DateOnly, List<TimedRecord>>();
//    var stationName = station.FileName;

//    logger.LogInformation($"About to being processing {stationName} from {station.Country}.");

//    var missingYears = new List<int>();
//    for (var year = station.Begin.Year; year <= station.End.Year; year++)
//    {
//        logger.LogInformation($"Checking year {year} of {stationName}");

//        var fileName = $"{stationName}-{year}";
//        var extractedFile = $@"Output\Isd\{stationName}\{fileName}.txt";

//        if (!File.Exists(extractedFile))
//        {
//            var success = await IsdDownloadAndExtract.DownloadAndExtractFile(httpClient, year, stationName, fileName, logger);
//            if (!success)
//            {
//                logger.LogWarning($"Failed to download or extract ISD file for {year} of {stationName}. No data will be available for this year.");
//            }
//        }

//        var recordsForYear = File.ReadAllLines(extractedFile);
//        var failedYear = false;
//        if (recordsForYear[0].StartsWith("Failed"))
//        {
//            failedYear = true;
//            missingYears.Add(year);
//        }

//        var transformedRecordsForYear = IsdFileProcessor.Transform(recordsForYear, logger);
//        transformedRecordsForYear.Keys
//            .ToList()
//            .ForEach(x => dataRecords.Add(x, transformedRecordsForYear[x]));

//        // Will only delete the extracted gz file from NOAA for files that have downloaded and extracted successfully
//        // This will prevent us from pestering NOAA to try again to download the file because we have stub file saying "Failed"
//        if (File.Exists(extractedFile) && !failedYear)
//        {
//            logger.LogInformation($"Deleting file {fileName}.txt");
//            File.Delete(extractedFile);
//        }
//    }
//    return (dataRecords, missingYears);
//}