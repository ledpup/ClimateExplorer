﻿using ClimateExplorer.WebApiClient.Services;
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

logger.LogInformation("Creating locations cache");
var locations = await dataService.GetLocations();
logger.LogInformation("Finished creating locations cache");

await Parallel.ForEachAsync(locations!, new ParallelOptions(), async (location, token) => 
{
    logger.LogInformation($"Creating climate records for {location.Name}");
    await dataService.GetClimateRecords(location.Id);
    logger.LogInformation($"Finished creating climate records for {location.Name}");
});