using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Logging;

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger("Program");
logger.LogInformation("Hello World! Logging is {Description}.", "fun");


var longtermCache = @"..\..\..\..\ClimateExplorer.WebApi\cache-longterm";

// Delete the content cache files while retaining the key files (those that start with "hash_")
// This CachingTool will then regenerate the content file based on the latest data.
var files = Directory.GetFiles(longtermCache);
var filesToDelete = files.Where(x => !x.Contains("hash_")).ToList();
filesToDelete.ForEach(File.Delete);

var dscLogger = factory.CreateLogger<DataServiceCache>();
var dataService = new DataService(new HttpClient() { BaseAddress = new Uri("http://localhost:54836/") }, new DataServiceCache(dscLogger));

// Get the basic list of locations. We need this to calculate the climate records
var locations = await dataService.GetLocations(permitCreateCache: false);

await Parallel.ForEachAsync(locations!, new ParallelOptions(), async (location, token) =>
{
    logger.LogInformation($"Creating climate records for {location.Name}");
    await dataService.GetClimateRecords(location.Id);
    logger.LogInformation($"Finished creating climate records for {location.Name}");
});

// Generate cached locations. This will include the RecordHigh climate record
logger.LogInformation("Creating locations cache");
locations = await dataService.GetLocations();
logger.LogInformation("Finished creating locations cache");