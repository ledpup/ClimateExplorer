using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClimateExplorer.Data.IntegratedSurfaceData;

var serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(x => { x.SingleLine = true; x.IncludeScopes = false; })
        )
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger<Program>();
var isdFileProcessorLogger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger<IsdFileProcessor>();

var httpClient = new HttpClient();
var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

var filteredStations = await RetrieveFilteredStations();

foreach (var station in filteredStations)//.Where(x => x.Name.Contains("LAUNCESTON")))
{
    //logger.BeginScope(station.Name);

    if (File.Exists($@"Output\ProcessedRecords\{station.FileName}.csv"))
    {
        logger.LogInformation($"Station file {station.FileName}.csv already exists. Download and processing of ISD file will not be done.");
        continue;
    }

    var results = await GetStationRecords(httpClient, station, logger, isdFileProcessorLogger);
    if (results.MissingYears.Any())
    {
        logger.LogWarning($"There are {results.MissingYears.Count} missing years for {station.FileName} where data is expected from {station.Begin.Year} to {station.End.Year}. They are: { string.Join(',', results.MissingYears) }");
    }
    DataRecordFileSaver.Save(station.FileName, results.Records, logger);
}

async Task<List<Station>> RetrieveFilteredStations()
{
    var countries = await CountryFileProcessor.Transform();

    var stations = await StationHistoryFileProcessor.Transform(countries, 1950, (short)(DateTime.Now.Year - 10));
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

static async Task<(Dictionary<DateOnly, List<TimedRecord>> Records, List<int> MissingYears)> GetStationRecords(HttpClient httpClient, Station station, ILogger<Program> logger, ILogger<IsdFileProcessor> isdFileProcessorLogger)
{
    var dataRecords = new Dictionary<DateOnly, List<TimedRecord>>();
    var stationName = station.FileName;

    logger.LogInformation($"About to being processing {stationName} from {station.Country}.");

    var missingYears = new List<int>();
    for (var year = station.Begin.Year; year <= station.End.Year; year++)
    {
        logger.LogInformation($"Checking year {year} of {stationName}");

        var fileName = $"{stationName}-{year}";
        var extractedFile = $@"Output\Isd\{stationName}\{fileName}.txt";

        if (!File.Exists(extractedFile))
        {
            var success = await IsdDownloadAndExtract.DownloadAndExtractFile(httpClient, year, stationName, fileName, logger);
            if (!success)
            {
                logger.LogWarning($"Failed to download or extract ISD file for {year} of {stationName}. No data will be available for this year.");
            }
        }
      
        var recordsForYear = File.ReadAllLines(extractedFile);
        var failedYear = false;
        if (recordsForYear[0].StartsWith("Failed"))
        {
            failedYear = true;
            missingYears.Add(year);
        }

        var transformedRecordsForYear = IsdFileProcessor.Transform(recordsForYear, isdFileProcessorLogger);
        transformedRecordsForYear.Keys
            .ToList()
            .ForEach(x => dataRecords.Add(x, transformedRecordsForYear[x]));

        // Will only delete the extracted gz file from NOAA for files that have downloaded and extracted successfully
        // This will prevent us from pestering NOAA to try again to download the file because we have stub file saying "Failed"
        if (File.Exists(extractedFile) && !failedYear)
        {
            logger.LogInformation($"Deleting file {fileName}.txt");
            File.Delete(extractedFile);
        }
    }
    return (dataRecords, missingYears);
}