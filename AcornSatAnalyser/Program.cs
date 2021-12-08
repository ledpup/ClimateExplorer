using System.Linq;
using System.Text.RegularExpressions;

var locations = new Dictionary<string, string>();
locations.Add("Launceston Airport", "091311");
locations.Add("Kerang", "080023");

var primarySites = File.ReadAllLines(@$"primarysites.txt");

var siteSets = new Dictionary<string, List<string>>();

foreach (var primarySiteRow in primarySites)
{
    var primarySite = primarySiteRow.Substring(0, 6);
    var firstSite = primarySiteRow.Substring(7, 6);
    var secondSite = primarySiteRow.Substring(32, 6);
    var thirdSite = primarySiteRow.Substring(57, 6);

    siteSets.Add(primarySite, new List<string>
    {
        firstSite
    });

    if (secondSite != "999999")
    {
        siteSets[primarySite].Add(secondSite);
        if (thirdSite != "999999")
        {
            siteSets[primarySite].Add(thirdSite);
        }
    }
}

var minNumberOfRecordsForTheYear = 355;
var averageTempsFileNameSuffix = "yearly average temps";
var tempsRegEx = new Regex(@"\s+(-*\d+)\s+(-*\d+)");

foreach (var location in locations)
{
    var siteSet = siteSets[location.Value];

    var folderName = location.Key.Replace(" ", "_");

    var earliestYearInSiteSet = 3000;
    var latestYearInSiteSet = 0;

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
                WriteYearData(currentYear, dailyMinData, dailyMaxData, yearMinData, yearMaxData, siteFile);

                currentYear = year;
                dailyMinData = new List<float>();
                dailyMaxData = new List<float>();
            }

            var month = int.Parse(record.Substring(10, 2));
            var day = int.Parse(record.Substring(12, 2));
            var temps = record.Substring(13);
            var tempGroups = tempsRegEx.Match(temps).Groups;
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

        WriteYearData(currentYear, dailyMinData, dailyMaxData, yearMinData, yearMaxData, siteFile);
    }

    var siteSetYear = new Dictionary<int, List<SiteTemps>>();
    {
        var year = earliestYearInSiteSet;
        while (year <= latestYearInSiteSet)
        {
            siteSetYear.Add(year, new List<SiteTemps>());
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
                siteSetYear[year].Add(new SiteTemps { Min = min, Max = max, Name = site });
            }
        }
    }

    using StreamWriter siteSetFile = new(@$"{folderName}\{location.Key} {averageTempsFileNameSuffix}.csv");
    siteSetFile.WriteLine($"Year,YearAverageMin,YearAverageMax,NumberOfSites,Sites");

    foreach (var year in siteSetYear.Keys)
    {
        if (siteSetYear[year].Any())
        {
            var min = siteSetYear[year].Average(x => x.Min);
            var max = siteSetYear[year].Average(x => x.Max);
            siteSetFile.WriteLine($"{year},{min},{max},{siteSetYear[year].Count},\"=\"\"{string.Join(';', siteSetYear[year].Select(x => x.Name))}\"\"\"");
        }
    }
}

void WriteYearData(int year, List<float> dailyMinData, List<float> dailyMaxData, Dictionary<int, double> yearMinData, Dictionary<int, double> yearMaxData, StreamWriter siteFile)
{
    if (year != 0 && dailyMinData.Count > 0 && dailyMaxData.Count > 0)
    {
        yearMinData.Add(year, dailyMinData.Average());
        yearMaxData.Add(year, dailyMaxData.Average());

        siteFile.WriteLine($"{year},{yearMinData[year]},{dailyMinData.Count},{yearMaxData[year]},{dailyMaxData.Count}");
    }
}