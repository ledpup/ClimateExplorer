using System.Text.Json;

namespace ClimateExplorer.Data.Greenland;

public class GreenlandApiClient
{
    public static async Task GetMeltDataAndSave(HttpClient httpClient)
    {
        var sourcePath = @$"Output\Data\Greenland\Years";

        for (var year = 1979; year <= DateTime.Now.Year; year++)
        {
            await DownloadAndExtractData(httpClient, year, sourcePath);
        }

        var meltRecords = new List<string>();

        for (var year = 1979; year < DateTime.Now.Year; year++)
        {
            var dataFile = $"{year}_greenland.json";
            var csvFilePathAndName = @$"{sourcePath}\{dataFile}";

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

        var destinationPath = @$"Output\Data\Greenland\greenland-melt-area.csv";
        File.WriteAllLines(destinationPath, meltRecords);
    }


    async static Task DownloadAndExtractData(HttpClient httpClient, int year, string filePath)
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
            return;
        }

        var url = $"https://nsidc.org/api/greenland/melt_area/{year}";
        var response = await httpClient.GetAsync(url);

        var content = await response.Content.ReadAsStringAsync();

        await File.WriteAllTextAsync(csvFilePathAndName, content);
    }
}
