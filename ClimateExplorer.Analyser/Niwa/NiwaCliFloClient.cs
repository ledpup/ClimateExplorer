using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClimateExplorer.Analyser.Niwa;

public class NiwaCliFloClient
{
    public static async Task GetDataForEachStation(List<Station> stations)
    {
        var dataTypes = new List<(string Name, string ObsCode, string Columns)>
        {
            ("rainfall", "181", "ls_ra,1,2,3,4"), // 181 is rainfall. dt1 describes the columns that are returned
            //("temperature", "201", "ls_mxmn,1,2,3,4,5,6") // 201 is temperature.
        };

        foreach (var station in stations)
        {
            foreach (var dataType in dataTypes)
            {
                await DownloadAndExtractDailyBomData(station.ExternalStationCode, dataType.Name, dataType.ObsCode, dataType.Columns);
            }
        }
    }


    async static Task DownloadAndExtractDailyBomData(string station, string dataTypeName, string obsCode, string columns)
    {
        var dataFile = $"{station}_{dataTypeName.ToString().ToLower()}";
        var filePath = @$"Output\Data\{dataTypeName}";

        var dir = new DirectoryInfo(filePath);
        if (!dir.Exists)
        {
            dir.Create();
        }

        var csvFilePathAndName = @$"{filePath}\{dataFile}.csv";

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
        httpClient.DefaultRequestHeaders.Add("cookie", "PCID=AE8E675699B91ACD1CC1C07E50061D6F7697F41DE372695FE33CE4A681FA31F160421FE97DB72B9C; UserID=DDD43878A3FDAB961ABFBFAD1C4907DDFB553EEFF1E13290; _ga=GA1.3.872675034.1656568800; _gid=GA1.3.1938993.1663388460; cebs=1; _ce.s=v~e8505d8928c7568a99d62ae282809d9c3b026bc7~vpv~0; cebsp=1");
        httpClient.DefaultRequestHeaders.Add("origin", "https://cliflo.niwa.co.nz");
        httpClient.DefaultRequestHeaders.Add("referer", "https://cliflo.niwa.co.nz/pls/niwp/wgenf.genform1?cdt=ls_ra&cdel=t");

        var startYear = 1940;
        var endYear = DateTime.Now.Year;

        var dictionary = new Dictionary<string, string>
            {
                { "cselect", "wgenf.genform1?fset=defdtype" },
                { "prm1", obsCode },
                { "dt1",  columns },
                { "auswahl", "wgenf.genform1?fset=defagent" },
                { "agents", station },
                { "dateauswahl", "wgenf.genform1?fset=defdate" },
                { "date1_1", startYear.ToString() },
                { "date1_2", "01" },
                { "date1_3", "01" },
                { "date1_4", "00" },
                { "date2_1", endYear.ToString() },
                { "date2_2", "12" },
                { "date2_3", "31" },
                { "date2_4", "00" },
                { "formatselection", "wgenf.genform1?fset=deffmt" },
                { "TSselection", "UTC" },
                { "dateformat", "0" },
                { "Splitdate", "N" },
                { "mimeselection", "csvplain" },
                { "cstn_id", "A" },
                { "cdata_order", "DS" },
                { "submit_sq", "Send Query" },
            };

        var keyValues = dictionary.ToList();
        var formContent = new FormUrlEncodedContent(keyValues);

        var url = $"https://cliflo.niwa.co.nz/pls/niwp/wgenf.genform1_proc";
        var response = await httpClient.PostAsync(url, formContent);

        var buffer = await response.Content.ReadAsByteArrayAsync();
        var byteArray = buffer.ToArray();
        var content = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

        await File.WriteAllTextAsync(csvFilePathAndName, content);
    }
}
