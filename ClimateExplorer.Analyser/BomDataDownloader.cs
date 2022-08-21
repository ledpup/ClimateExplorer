using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClimateExplorer.Analyser;

public class BomDataDownloader
{
    public static async Task GetDataForEachStation(List<Station> stations)
    {
        var obsCodes = Enum.GetValues(typeof(ObsCode)).Cast<ObsCode>().ToList();
        foreach (var station in stations)
        {
            foreach (var obsCode in obsCodes)
            {
                await DownloadAndExtractDailyBomData(station.ExternalStationCode, obsCode);
            }
        }
    }

    async static Task DownloadAndExtractDailyBomData(string station, ObsCode obsCode)
    {
        var dataFile = $"{station}_{obsCode.ToString().ToLower()}";
        var zipfileName = dataFile + ".zip";
        var csvFilePathAndName = @$"{obsCode.ToString().ToLower()}\{dataFile}.csv";

        // If we've already downloaded and extracted the csv, let's not do it again.
        // Prevents hammering the BOM when we already have the data.
        if (File.Exists(csvFilePathAndName))
        {
            return;
        }

        var regEx = new Regex(@"\d{6}\|\|,(?<startYear>\d{4}):(?<p_c>-?\d+),");
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
        var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

            // This section is required to find the arcane p_c value, needed for the querystring of the dailyZippedDataFile request, that returns the daily data as a zip file
            var availableYearsUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/wData/wdata?p_stn_num={station}&p_display_type=availableYears&p_nccObsCode={(int)obsCode}";
            var response = await httpClient.GetAsync(availableYearsUrl);

            var responseContent = await response.Content.ReadAsStringAsync();
            var match = regEx.Match(responseContent);
            var p_c = match.Groups["p_c"].Value;
            var startYear = match.Groups["startYear"].Value;

            if (string.IsNullOrWhiteSpace(startYear))
            {
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
                var destinationDirectory = new DirectoryInfo(obsCode.ToString().ToLower());
                if (!destinationDirectory.Exists)
                {
                    destinationDirectory.Create();
                }
                csv.MoveTo(csvFilePathAndName, true);

                // Remove the temp directory
                DeleteDirectory(tempDirectory);
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine($"Unable to extract zip file {zipfileName}. File may be corrupt. Message: {ex.Message}");
            }
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
