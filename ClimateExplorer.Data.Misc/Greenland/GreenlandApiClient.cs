namespace ClimateExplorer.Data.Misc;

using Microsoft.Extensions.Logging;
using System.Text.Json;

public class GreenlandApiClient
{
    public static async Task GetMeltDataAndSave(HttpClient httpClient, ILogger logger)
    {
        var downloadPath = @$"Output\Greenland";

        logger.LogInformation("Downloading Greenland data to {downloadPath}", downloadPath);

        var endYear = DateTime.Now.Year - 1; // Don't download current year - it's unlikely to be complete.
        for (var year = 1979; year <= endYear; year++)
        {
            logger.LogInformation("Downloading {year}", year);
            await DownloadAndExtractData(httpClient, year, downloadPath, logger);
        }

        var meltRecords = new List<string>();

        for (var year = 1979; year < DateTime.Now.Year; year++)
        {
            var dataFile = $"{year}_greenland.json";
            var csvFilePathAndName = @$"{downloadPath}\{dataFile}";

            var json = await File.ReadAllTextAsync(csvFilePathAndName);

            var records = JsonSerializer.Deserialize<Dictionary<string, double?>>(json);

            var begin = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);

            for (var date = begin; date <= end; date = date.AddDays(1))
            {
                var key = date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var outputDate = date.ToString("yyyy'-'MM'-'dd");
                if (records!.ContainsKey(key))
                {
                    meltRecords.Add($"{outputDate},{records[key]}");
                }
                else
                {
                    meltRecords.Add($"{outputDate},0");
                }
            }
        }

        var fileName = "greenland-melt-area.csv";
        var destinationPath = Path.Combine(Helpers.SourceDataFolder, "Ice", fileName);

        logger.LogInformation("Saving file {fileName} to {destinationPath}", fileName, destinationPath);

        File.WriteAllLines(destinationPath, meltRecords);
    }

    private static async Task DownloadAndExtractData(HttpClient httpClient, int year, string filePath, ILogger logger)
    {
        var dataFile = $"{year}_greenland.json";

        var dir = new DirectoryInfo(filePath);
        if (!dir.Exists)
        {
            dir.Create();
        }

        var csvFilePathAndName = @$"{filePath}\{dataFile}";

        // If we've already downloaded and extracted the csv, let's not do it again.
        if (File.Exists(csvFilePathAndName))
        {
            logger.LogInformation("{year} file already exists. Will not download again.", year);
            return;
        }

        var url = $"https://nsidc.org/api/greenland/melt_area/{year}";
        var response = await httpClient.GetAsync(url);

        var content = await response.Content.ReadAsStringAsync();

        await File.WriteAllTextAsync(csvFilePathAndName, content);
    }
}
