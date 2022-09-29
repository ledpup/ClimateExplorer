using ClimateExplorer.Core.DataPreparation.Model;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClimateExplorer.Analyser.Greenland;

public class GreenlandApiClient
{
    public static async Task GetMeltDataAndSave()
    {
        var sourcePath = @$"Output\Data\Greenland\Years";

        for (var year = 1979; year <= DateTime.Now.Year; year++)
        {
            await DownloadAndExtractDailyBomData(year, sourcePath);
        }

        var meltRecords = new List<string>();

        for (var year = 1979; year <= DateTime.Now.Year; year++)
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
                if (records.ContainsKey(key))
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


    async static Task DownloadAndExtractDailyBomData(int year, string filePath)
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

        using var httpClient = new HttpClient();
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

        var url = $"https://nsidc.org/api/greenland/melt_area/{year}";
        var response = await httpClient.GetAsync(url);

        var content = await response.Content.ReadAsStringAsync();

        await File.WriteAllTextAsync(csvFilePathAndName, content);
    }
}
