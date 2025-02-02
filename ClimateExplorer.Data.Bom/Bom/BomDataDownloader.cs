﻿namespace ClimateExplorer.Data.Bom;

using ClimateExplorer.Core.Model;
using System.IO.Compression;
using System.Text.RegularExpressions;

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

    async static Task DownloadAndExtractDailyBomData(HttpClient httpClient, string station, ObsCode obsCode, string outputDirectory)
    {
        var dataFile = $"{station}_{obsCode.ToString().ToLower()}";
        var zipfileName = @$"Output\Temp\{dataFile}.zip";
        var csvFilePathAndName = @$"{outputDirectory}\{dataFile}.csv";

        // If we've already downloaded the zip, let's not do it again.
        if (File.Exists(zipfileName))
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Skipping {zipfileName} as it already exists.");
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }

        var regEx = new Regex(@"\d{6}\|\|,(?<startYear>\d{4}):(?<p_c>-?\d+),");

        // This section is required to find the arcane p_c value, needed for the querystring of the dailyZippedDataFile request, that returns the daily data as a zip file
        var availableYearsUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/wData/wdata?p_stn_num={station}&p_display_type=availableYears&p_nccObsCode={(int)obsCode}";
        var response = await httpClient.GetAsync(availableYearsUrl);

        var responseContent = await response.Content.ReadAsStringAsync();
        var match = regEx.Match(responseContent);
        var p_c = match.Groups["p_c"].Value;
        var startYear = match.Groups["startYear"].Value;

        if (string.IsNullOrWhiteSpace(startYear))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unable to find a start year so skipping ObsCode {obsCode} for station {station}");
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }

        // The response of this request is a zip file that needs to be downloaded, extracted and named in a form that we'll be able to find it again by station number
        var zipFileUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_display_type=dailyZippedDataFile&p_stn_num={station}&p_nccObsCode={(int)obsCode}&p_c={p_c}&p_startYear={startYear}";
        var zipFileResponse = await httpClient.GetAsync(zipFileUrl);
        using (var fs = new FileStream(zipfileName, FileMode.OpenOrCreate))
        {
            await zipFileResponse.Content.CopyToAsync(fs);
        }

        var tempDirectory = new DirectoryInfo("temp");
        DeleteDirectory(tempDirectory);
        try
        {
            // Extract the zip file with the daily data to a folder called temp
            ZipFile.ExtractToDirectory(zipfileName, "temp");

            // Find the csv file with the data and move and rename it, putting it in the output folder (named based on the observation code)
            var csv = tempDirectory.GetFiles("*.csv").Single();
            
            csv.MoveTo(csvFilePathAndName, true);

            // Remove the temp directory
            DeleteDirectory(tempDirectory);
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"Unable to extract zip file {zipfileName}. File may be corrupt. Message: {ex.Message}");
        }
    }

    static void DeleteDirectory(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Exists)
        {
            directoryInfo.GetFiles().ToList().ForEach(file => file.Delete());
            directoryInfo.Delete();
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
