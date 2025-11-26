using ClimateExplorer.Data.Bom;
using static ClimateExplorer.Data.Bom.BomDataDownloader;

var httpClient = new HttpClient();
var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

const string SourceDataFolder = @"..\..\..\..\ClimateExplorer.SourceData";

AcornSatDownloader.DownloadAndExtractData("acorn_sat_v2.6.0_daily_tmean", $@"{SourceDataFolder}\Temperature\ACORN-SAT\daily_tmean", new Dictionary<string, string>{ { "tmean.060168.daily.csv", "tmean.060139.daily.csv" } });
AcornSatDownloader.DownloadAndExtractData("acorn_sat_v2.6.0_daily_tmax", $@"{SourceDataFolder}\Temperature\ACORN-SAT\daily_tmax", new Dictionary<string, string> { { "tmax.060168.daily.csv", "tmax.060139.daily.csv" } });
AcornSatDownloader.DownloadAndExtractData("acorn_sat_v2.6.0_daily_tmin", $@"{SourceDataFolder}\Temperature\ACORN-SAT\daily_tmin", new Dictionary<string, string> { { "tmin.060168.daily.csv", "tmin.060139.daily.csv" } });

var outDirectories = new Dictionary<ObsCode, string>
    {
        { ObsCode.Daily_TempMax, $@"{SourceDataFolder}\Temperature_BOM\daily_tempmax" }, 
        { ObsCode.Daily_TempMin, $@"{SourceDataFolder}\Temperature_BOM\daily_tempmin" }, 
        { ObsCode.Daily_Rainfall, $@"{SourceDataFolder}\Precipitation\BOM" },
        { ObsCode.Daily_SolarRadiation, $@"{SourceDataFolder}\Solar\BOM" },
    };

/*
 * 
 * 
 * Ensure you delete the contents of bin\Debug\netX\Output\Temp if you're running it again in the new year with an unchanged .NET version
 * 
 * Directory.Delete(@$"Output\Temp");
 * 
 * 
 */

var stations = await BomLocationsAndStationsMapper.BuildAcornSatLocationsFromReferenceMetaDataAsync(Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"), "_Australia_unadjusted");
await GetDataForEachStation(httpClient, stations, outDirectories);
await BomLocationsAndStationsMapper.BuildAcornSatAdjustedDataFileMappingAsync(Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"), @"Output\DataFileMapping\DataFileMapping_Australia_unadjusted.json", "_Australia_adjusted");