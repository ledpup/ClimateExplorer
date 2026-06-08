namespace ClimateExplorer.Data.Bom;

using ClimateExplorer.Core.Model;
using System.IO.Compression;
using System.Text.RegularExpressions;
using static ClimateExplorer.Core.Enums;

public class BomDataDownloader
{
    public static async Task GetDataForEachStation(HttpClient httpClient, List<Station> stations, Dictionary<ObsCode, string> outputDirectories)
    {
        if (!Directory.Exists(@$"Output\Temp"))
        {
            Directory.CreateDirectory(@$"Output\Temp");
        }

        var obsCodes = Enum.GetValues<ObsCode>().Cast<ObsCode>().ToList();
        foreach (var station in stations)
        {
            Console.WriteLine($"Processing station {station.Id}");
            foreach (var obsCode in obsCodes)
            {
                Console.WriteLine($"Processing ObsCode {obsCode} for station {station.Id}");

                await DownloadAndExtractDailyBomData(httpClient, station.Id, obsCode, outputDirectories[obsCode]);

                Console.WriteLine($"Finished ObsCode {obsCode} for station {station.Id}");
            }
            Console.WriteLine($"Finished station {station.Id}");
            Console.WriteLine();
        }
    }

    public static async Task<string?> DownloadAndExtractDailyBomData(
        HttpClient httpClient,
        Guid locationId,
        DataType dataType,
        DataFileMapping dataFileMapping,
        string outputDirectory,
        int? startYear = null,
        string? fileNameSuffix = null,
        bool overwriteExistingZip = false)
    {
        var stationId = GetMostRecentOperatingStationId(locationId, dataFileMapping);
        if (stationId == null)
        {
            return null;
        }

        return await DownloadAndExtractDailyBomData(httpClient, stationId, dataType, outputDirectory, startYear, fileNameSuffix, overwriteExistingZip);
    }

    public static async Task<string?> DownloadAndExtractDailyBomData(
        HttpClient httpClient,
        string station,
        DataType dataType,
        string outputDirectory,
        int? startYear = null,
        string? fileNameSuffix = null,
        bool overwriteExistingZip = false)
    {
        return await DownloadAndExtractDailyBomData(httpClient, station, dataType.ToObsCode(), outputDirectory, startYear, overwriteExistingZip);
    }

    public static async Task<string?> DownloadAndExtractDailyBomData(
        HttpClient httpClient,
        string station,
        ObsCode obsCode,
        string outputDirectory,
        int? startYear = null,
        bool overwriteExistingZip = false)
    {
        var dataFile = $"{station}_{obsCode.ToString().ToLower()}";

        Directory.CreateDirectory(outputDirectory);

        var tempDirectoryInfo = new DirectoryInfo(@"Output\Temp\");
        if (!tempDirectoryInfo.Exists)
        {
            tempDirectoryInfo.Create();
        }

        var zipfileName = Path.Combine(tempDirectoryInfo.FullName, $"{dataFile}.zip");
        var csvFilePathAndName = Path.Combine(outputDirectory, $"{dataFile}.csv");

        // If we've already downloaded the zip, let's not do it again.
        if (File.Exists(zipfileName) && !overwriteExistingZip)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Skipping {zipfileName} as it already exists.");
            Console.ForegroundColor = ConsoleColor.Gray;
            return File.Exists(csvFilePathAndName) ? csvFilePathAndName : null;
        }

        var regEx = new Regex(@"\d{6}\|\|,(?<startYear>\d{4}):(?<p_c>-?\d+),");

        // This section is required to find the arcane p_c value, needed for the querystring of the dailyZippedDataFile request, that returns the daily data as a zip file
        var availableYearsUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/wData/wdata?p_stn_num={station}&p_display_type=availableYears&p_nccObsCode={(int)obsCode}";
        var response = await httpClient.GetAsync(availableYearsUrl);

        var responseContent = await response.Content.ReadAsStringAsync();
        var match = regEx.Match(responseContent);
        if (!match.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unable to find available years for ObsCode {obsCode} for station {station}");
            Console.ForegroundColor = ConsoleColor.Gray;
            return null;
        }

        var p_c = match.Groups["p_c"].Value;
        startYear = startYear ?? int.Parse(match.Groups["startYear"].Value);

        if (startYear is null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unable to find a start year so skipping ObsCode {obsCode} for station {station}");
            Console.ForegroundColor = ConsoleColor.Gray;
            return null;
        }

        // The response of this request is a zip file that needs to be downloaded, extracted and named in a form that we'll be able to find it again by station number
        var zipFileUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_display_type=dailyZippedDataFile&p_stn_num={station}&p_nccObsCode={(int)obsCode}&p_c={p_c}&p_startYear={startYear}";
        var zipFileResponse = await httpClient.GetAsync(zipFileUrl);
        using (var fs = new FileStream(zipfileName, FileMode.Create))
        {
            await zipFileResponse.Content.CopyToAsync(fs);
        }

        var extractDirectory = new DirectoryInfo(Path.Combine(tempDirectoryInfo.FullName, $"{dataFile}_{Guid.NewGuid():N}"));
        DeleteDirectory(extractDirectory);
        try
        {
            // Extract the zip file with the daily data to a folder called temp
            ZipFile.ExtractToDirectory(zipfileName, extractDirectory.FullName);

            // Find the csv file with the data and move and rename it, putting it in the output folder (named based on the observation code)
            var csv = extractDirectory.GetFiles("*.csv").Single();
            
            csv.MoveTo(csvFilePathAndName, true);

            // Remove the temp directory
            DeleteDirectory(extractDirectory);
            return csvFilePathAndName;
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"Unable to extract zip file {zipfileName}. File may be corrupt. Message: {ex.Message}");
            return null;
        }
    }

    public static string? GetMostRecentOperatingStationId(Guid locationId, DataFileMapping dataFileMapping, DateOnly? asAt = null)
    {
        if (!dataFileMapping.LocationIdToDataFileMappings.TryGetValue(locationId, out var dataFileFilterAndAdjustments))
        {
            return null;
        }

        var comparisonDate = asAt ?? DateOnly.FromDateTime(DateTime.Today);
        return dataFileFilterAndAdjustments
            .Where(x => (!x.StartDate.HasValue || x.StartDate.Value <= comparisonDate) && (!x.EndDate.HasValue || x.EndDate.Value >= comparisonDate))
            .OrderByDescending(x => x.StartDate ?? DateOnly.MinValue)
            .FirstOrDefault()
            ?.Id;
    }

    static void DeleteDirectory(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Exists)
        {
            directoryInfo.Delete(true);
        }
    }

    public enum ObsCode
    {
        Daily_TempMax = 122,
        Daily_TempMin = 123,
        Daily_Rainfall = 136,
        Daily_SolarRadiation = 193,

        //Unknown1 = 124,
        //Unknown2 = 125,
        //Unknown3 = 126,
        //Unknown4 = 127,
        //Unknown5 = 128,
        //Unknown6 = 129,
        //Unknown7 = 131,
        //Unknown8 = 133,
    }
}

public static class BomDataTypeExtensions
{
    public static BomDataDownloader.ObsCode ToObsCode(this DataType dataType)
    {
        return dataType switch
        {
            DataType.TempMax => BomDataDownloader.ObsCode.Daily_TempMax,
            DataType.TempMin => BomDataDownloader.ObsCode.Daily_TempMin,
            DataType.Precipitation => BomDataDownloader.ObsCode.Daily_Rainfall,
            DataType.SolarRadiation => BomDataDownloader.ObsCode.Daily_SolarRadiation,
            _ => throw new NotSupportedException($"{dataType} is not supported by BOM daily downloads."),
        };
    }

    public static bool TryToObsCode(this DataType dataType, out BomDataDownloader.ObsCode obsCode)
    {
        switch (dataType)
        {
            case DataType.TempMax:
            case DataType.TempMin:
            case DataType.Precipitation:
            case DataType.SolarRadiation:
                obsCode = dataType.ToObsCode();
                return true;

            default:
                obsCode = default;
                return false;
        }
    }
}
