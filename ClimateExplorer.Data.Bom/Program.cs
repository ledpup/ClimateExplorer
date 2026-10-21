using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Bom;
using ClimateExplorer.Data.Downloading.Downloaders;

const string SourceDataFolder = @"..\..\..\..\ClimateExplorer.SourceData";

AcornSatDownloader.DownloadAndExtractData("acorn_sat_v2.7.0_daily_tmean", $@"{SourceDataFolder}\Temperature\ACORN-SAT\daily_tmean");
AcornSatDownloader.DownloadAndExtractData("acorn_sat_v2.7.0_daily_tmax", $@"{SourceDataFolder}\Temperature\ACORN-SAT\daily_tmax");
AcornSatDownloader.DownloadAndExtractData("acorn_sat_v2.7.0_daily_tmin", $@"{SourceDataFolder}\Temperature\ACORN-SAT\daily_tmin");

// File names (station_suffix.csv) are kept from the original downloader so existing consumers still find them
var outputs = new Dictionary<BomDailyObservationCode, (string Suffix, string Directory)>
    {
        { BomDailyObservationCode.TemperatureMaximum, ("daily_tempmax", $@"{SourceDataFolder}\Temperature_BOM\daily_tempmax") },
        { BomDailyObservationCode.TemperatureMinimum, ("daily_tempmin", $@"{SourceDataFolder}\Temperature_BOM\daily_tempmin") },
        { BomDailyObservationCode.Rainfall, ("daily_rainfall", $@"{SourceDataFolder}\Precipitation\BOM") },
        { BomDailyObservationCode.SolarRadiation, ("daily_solarradiation", $@"{SourceDataFolder}\Solar\BOM") },
    };

/*
 *
 *
 * Completed downloads are recorded in bin\Debug\netX\Output\Temp so an interrupted run resumes where it left off.
 * Delete the contents of that folder if you're running it again in the new year with an unchanged .NET version
 *
 * Directory.Delete(@$"Output\Temp");
 *
 *
 */

var stations = await BomLocationsAndStationsMapper.BuildAcornSatLocationsFromMetaDataAsync(Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"), "_Australia_unadjusted");
await DownloadDailyDataForEachStation(new BomDailyDataClient(BomHttpClientFactory.CreateBomHttpClient()), stations, outputs);
await BomLocationsAndStationsMapper.BuildAcornSatAdjustedDataFileMappingAsync(Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"), @"Output\DataFileMapping\DataFileMapping_Australia_unadjusted.json", "_Australia_adjusted");

static async Task DownloadDailyDataForEachStation(
    BomDailyDataClient client,
    List<Station> stations,
    Dictionary<BomDailyObservationCode, (string Suffix, string Directory)> outputs)
{
    // Pause between requests to keep the load on the BOM website light
    var delayBetweenDownloads = TimeSpan.FromSeconds(1);
    var completedDirectory = Directory.CreateDirectory(@"Output\Temp");

    foreach (var station in stations)
    {
        Console.WriteLine($"Processing station {station.Id}");
        foreach (var (observationCode, output) in outputs)
        {
            var dataFile = $"{station.Id}_{output.Suffix}";
            var completedMarker = Path.Combine(completedDirectory.FullName, $"{dataFile}.done");
            if (File.Exists(completedMarker))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Skipping {dataFile} as it has already been downloaded.");
                Console.ForegroundColor = ConsoleColor.Gray;
                continue;
            }

            Console.WriteLine($"Processing {observationCode} for station {station.Id}");
            try
            {
                var content = await client.DownloadCsvAsync(station.Id, observationCode, CancellationToken.None);
                Directory.CreateDirectory(output.Directory);
                await File.WriteAllTextAsync(Path.Combine(output.Directory, $"{dataFile}.csv"), content);
                await File.WriteAllTextAsync(completedMarker, string.Empty);
            }
            catch (InvalidDataException ex)
            {
                // Not every station records every observation type (e.g., solar radiation)
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unable to download {observationCode} for station {station.Id}. {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            await Task.Delay(delayBetweenDownloads);
        }

        Console.WriteLine($"Finished station {station.Id}");
        Console.WriteLine();
    }
}
