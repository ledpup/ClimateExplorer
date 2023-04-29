using ClimateExplorer.Data.Isd;
using System.IO.Compression;


var httpClient = new HttpClient();
var userAgent = "Mozilla /5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

var filteredStations = await RetrieveFilteredStations();

foreach (var station in filteredStations)//.Where(x => x.Name.Contains("LAUNCESTON")))
{
    for (var year = station.Begin.Year; year <= station.End.Year; year++)
    {
        var stationName = $"{station.Usaf}-{station.Wban}";
        var fileName = $"{stationName}-{year}";

        if (!File.Exists($@"Output\Isd\{stationName}\{fileName}"))
        {
            await DownloadAndExtractFile(httpClient, year, stationName, fileName);
        }

        var records = File.ReadAllLines($@"Output\Isd\{stationName}\{fileName}");
        var dataRecords = IsdFileProcessor.Transform(records);
        DataRecordFileSaver.Save(fileName.Substring(0, 6), dataRecords);
    }
}

async Task<List<Station>> RetrieveFilteredStations()
{
    var countries = await CountryFileProcessor.Transform();

    var stations = await StationHistoryFileProcessor.Transform(countries, 1950, (short)(DateTime.Now.Year - 10));
    var stationsGroupedByCountry = stations.Values.GroupBy(x => x.Country);

    var messages = new List<string>();

    foreach (var groupedStation in stationsGroupedByCountry.OrderByDescending(x => x.ToList().Count))
    {
        var stationList = groupedStation.ToList();

        messages.Add($"{(groupedStation.Key == null ? "No country" : groupedStation.Key.Name)} {stationList.Count} average age {Math.Round(groupedStation.Average(x => x.Age), 0)} years");

        if (stationList.Count > 1)
        {
            foreach (var station in stationList)
            {
                station.StationDistances = StationDistance.GetDistances(station, stationList);
                station.AverageDistance = station.StationDistances.Average(x => x.Distance);
            }
        }
        else
        {
            stationList.ForEach(x => x.AverageDistance = 1000 * 1000);
        }

        stationList.ForEach(x => x.Score = (x.Age / 10) * (x.AverageDistance / 1000D) * (1D / stationList.Count));
        stationList = stationList.OrderByDescending(x => x.Score).ToList();

        stationList.ForEach(x => messages.Add($"    {x.Id} score {x.Score}"));
    }


    messages.Add($"{stations.Count} stations from {stationsGroupedByCountry.Count()} countries");

    Directory.CreateDirectory("Output");
    File.WriteAllLines(@"Output\stations - unfiltered.txt", messages);

    var filteredStations = stations.Where(x => x.Value.Score >= 70).ToDictionary(x => x.Key, x => x.Value);

    messages = new List<string>();

    stationsGroupedByCountry = filteredStations.Values.GroupBy(x => x.Country);
    foreach (var groupedStation in stationsGroupedByCountry.OrderByDescending(x => x.ToList().Count))
    {
        var stationList = groupedStation.ToList();
        messages.Add($"{(groupedStation.Key == null ? "No country" : groupedStation.Key.Name)} {stationList.Count} average age {Math.Round(groupedStation.Average(x => x.Age), 0)} years");
    }

    messages.Add($"{filteredStations.Count} stations from {stationsGroupedByCountry.Count()} countries");

    File.WriteAllLines(@"Output\stations - filtered.txt", messages);

    return filteredStations.Values.ToList();
}

static async Task DownloadAndExtractFile(HttpClient httpClient, int year, string stationName, string fileName)
{
    var url = $"https://www.ncei.noaa.gov/pub/data/noaa/{year}/{fileName}.gz";
    var response = await httpClient.GetAsync(url);

    var content = await response.Content.ReadAsStreamAsync();

    Directory.CreateDirectory($@"Output\Isd\{stationName}");
    using (FileStream decompressedFileStream = File.Create($@"Output\Isd\{stationName}\{fileName}"))
    {
        using (GZipStream decompressionStream = new GZipStream(content, CompressionMode.Decompress))
        {
            decompressionStream.CopyTo(decompressedFileStream);
        }
    }
}