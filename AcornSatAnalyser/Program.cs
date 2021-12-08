using System.Linq;
using System.Text.RegularExpressions;

var siteSetName = "Launceston Airport";
var folderName = siteSetName.Replace(" ", "_");
var siteSet = new List<string>()
{
    "091049", "091104", "091311", 
};

var earliestYearInSiteSet = 3000;
var latestYearInSiteSet = 0;

var minNumberOfRecordsForTheYear = 360;

var averageTempsFileNameSuffix = "yearly average temps";

foreach (var site in siteSet)
{
    var rawData = File.ReadAllLines(@$"raw-data\hqnew{site}.txt");
    
    Directory.CreateDirectory(folderName);
    using StreamWriter siteFile = new(@$"{folderName}\{site} {averageTempsFileNameSuffix}.csv");
    siteFile.WriteLine($"Year,YearAverageMin,DaysOfDataMin,YearAverageMax,DaysOfDataMax");
    var currentYear = 0;

    var dailyMinData = new List<float>();
    var dailyMaxData = new List<float>();

    var yearMinData = new Dictionary<int, double>();
    var yearMaxData = new Dictionary<int, double>();

    foreach (var record in rawData)
    {
        var year = int.Parse(record.Substring(6, 4));

        if (year < earliestYearInSiteSet)
        {
            earliestYearInSiteSet = year;
        }

        if (year > latestYearInSiteSet)
        {
            latestYearInSiteSet = year;
        }

        if (year != currentYear)
        {
            if (dailyMinData.Count > 0 && dailyMaxData.Count > 0)
            {
                yearMinData.Add(year, dailyMinData.Average());
                yearMaxData.Add(year, dailyMaxData.Average());

                siteFile.WriteLine($"{year},{yearMinData[year]},{dailyMinData.Count},{yearMaxData[year]},{dailyMaxData.Count}");
            }

            currentYear = year;
            dailyMinData = new List<float>();
            dailyMaxData = new List<float>();
        }

        var month = int.Parse(record.Substring(10, 2));
        var day = int.Parse(record.Substring(12, 2));
        var temps = record.Substring(13);
        var regEx = new Regex(@"\s+(-*\d+)\s+(-*\d+)");
        var tempGroups = regEx.Match(temps).Groups;
        float? maxTemp = tempGroups[1].Value == "-999" ? null : float.Parse(tempGroups[1].Value) / 10;
        float? minTemp = tempGroups[2].Value == "-999" ? null : float.Parse(tempGroups[2].Value) / 10;

        if (minTemp != null)
        {
            dailyMinData.Add((float)minTemp.Value);
        }
        if (maxTemp != null)
        {
            dailyMaxData.Add((float)maxTemp.Value);
        }
    }
}

var siteSetYear = new Dictionary<int, List<Tuple<float, float, string>>>();
{
    var year = earliestYearInSiteSet;
    while (year <= latestYearInSiteSet)
    {
        siteSetYear.Add(year, new List<Tuple<float, float, string>>());
        year++;
    }
}

foreach (var site in siteSet)
{
    var rows = File.ReadAllLines(@$"{folderName}\{site} {averageTempsFileNameSuffix}.csv");
    var records = rows.Take(new Range(1, rows.Length));
    foreach (var record in records)
    {
        var data = record.Split(',');
        var year = int.Parse(data[0]);
        var min = float.Parse(data[1]);
        var numberOfRecordsMin = int.Parse(data[2]);

        var max = float.Parse(data[3]);
        var numberOfRecordsMax = int.Parse(data[4]);

        if (numberOfRecordsMin > minNumberOfRecordsForTheYear && numberOfRecordsMax > minNumberOfRecordsForTheYear)
        {
            siteSetYear[year].Add(new Tuple<float, float, string>(min, max, site));
        }
    }
}

using StreamWriter siteSetFile = new(@$"{folderName}\{siteSetName} {averageTempsFileNameSuffix}.csv");
siteSetFile.WriteLine($"Year,YearAverageMin,YearAverageMax,NumberOfSites,Sites");

foreach (var year in siteSetYear.Keys)
{
    if (siteSetYear[year].Any())
    {
        var min = siteSetYear[year].Average(x => x.Item1);
        var max = siteSetYear[year].Average(x => x.Item2);
        siteSetFile.WriteLine($"{year},{min},{max},{siteSetYear[year].Count},\"=\"\"{string.Join(';', siteSetYear[year].Select(x => x.Item3))}\"\"\"");
    }
}