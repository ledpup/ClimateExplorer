// See https://aka.ms/new-console-template for more information
using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

Console.WriteLine("Hello, World!");

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger("Program");
logger.LogInformation("Hello World! Logging is {Description}.", "fun");

var dscLogger = factory.CreateLogger<DataServiceCache>();

var dataService = new DataService(new HttpClient() { BaseAddress = new Uri("http://localhost:54836/") }, new DataServiceCache(dscLogger));

var longtermCache = @"..\..\..\..\ClimateExplorer.WebApi\cache-longterm";

if (Directory.Exists(longtermCache))
{
    Directory.Delete(longtermCache, true);
}
Directory.CreateDirectory(longtermCache);

logger.LogInformation("Creating locations cache");
var locations = await dataService.GetLocations();
logger.LogInformation("Finished creating locations cache");

await Parallel.ForEachAsync(locations!, new ParallelOptions(), async (location, token) => 
{
    logger.LogInformation($"Creating climate records for {location.Name}");
    await dataService.GetClimateRecords(location.Id);
    logger.LogInformation($"Finished creating climate records for {location.Name}");
});