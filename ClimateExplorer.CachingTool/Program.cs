using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Caching.Memory;
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

var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = DataService.CacheSizeLimit });
// The /location rebuild computes every location's stats in a single request, which can easily exceed the
// default 100 second HttpClient timeout.
var httpClient = new HttpClient() { BaseAddress = new Uri("http://localhost:54836/"), Timeout = Timeout.InfiniteTimeSpan };
var dataService = new DataService(httpClient, memoryCache);

// Generate cached locations (WarmingAnomaly, TemperatureTrends, RecordHigh, heating scores). This only reads the
// source files already published; it never downloads data - that is done by ClimateExplorer.Data.Misc.
logger.LogInformation("Creating locations cache");
await dataService.GetLocations(permitCreateCache: true);
logger.LogInformation("Finished creating locations cache");