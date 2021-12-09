using AcornSatAnalyser;
using System.Linq;
using System.Text.RegularExpressions;

Regex _tempsRegEx = new Regex(@"\s+(-*\d+)\s+(-*\d+)");

var locations = Location.GetLocations("Locations.csv");

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

var minNumberOfDailyRecordsForTheYear = args.Length > 0 ? string.IsNullOrWhiteSpace(args[0]) ? 355 : int.Parse(args[0]) : 355;
var averageDailyFileNameSuffix = $"yearly average from daily records. Threshold is {minNumberOfDailyRecordsForTheYear}";

var minNumberOfDailyRecordsForTheMonth = args.Length > 1 ? string.IsNullOrWhiteSpace(args[1]) ? 20 : int.Parse(args[0]) : 20;
var averageMonthlyFileNameSuffix = $"monthly average from daily average. Threshold of days in month is {minNumberOfDailyRecordsForTheMonth}";


foreach (var location in locations)
{
    var siteSet = siteSets[location.PrimarySite];

    var folderName = location.Name.Replace(" ", "_");

    var earliestYearInSiteSet = 3000;
    var latestYearInSiteSet = 0;

    var allSiteFilesExist = siteSet.All(x =>
    {
        var siteFilePath = @$"raw-data\hqnew{x}.txt";
        return File.Exists(siteFilePath);
    });

    if (!allSiteFilesExist)
    {
        Console.WriteLine($"Not all site files exist for {location.Name}. This location will be skipped. The missing files are: {string.Join(", ", siteSet.Where(x => !File.Exists(@$"raw-data\hqnew{x}.txt")).Select(x => @$"raw-data\hqnew{x}.txt"))} ");
        continue;
    }

    foreach (var site in siteSet)
    {
        Directory.CreateDirectory(folderName);

        var rawDataRecords = ReadRawDataFile(site);

        var minYear = rawDataRecords.Min(x => x.Year);
        var maxYear = rawDataRecords.Max(x => x.Year);

        if (minYear < earliestYearInSiteSet)
        {
            earliestYearInSiteSet = minYear;
        }
        if (maxYear > latestYearInSiteSet)
        {
            latestYearInSiteSet = maxYear;
        }

        using StreamWriter siteDailyOutputFile = new(@$"{folderName}\{site} {averageDailyFileNameSuffix}.csv");
        siteDailyOutputFile.WriteLine($"Year,YearAverageMin,DaysOfDataMin,YearAverageMax,DaysOfDataMax");

        using StreamWriter siteMonthlyOutputFile = new(@$"{folderName}\{site} {averageMonthlyFileNameSuffix}.csv");
        siteMonthlyOutputFile.WriteLine($"Year,Month,MonthAverageMin,DaysOfDataMin,MonthAverageMax,DaysOfDataMax");

        rawDataRecords.GroupBy(x => x.Year)
                        .Select(x => x.ToList())
                        .ToList()
                        .ForEach(x =>
                        { 
                            WriteYearData(x.First().Year, x, siteDailyOutputFile);
                            WriteMonthData(x.First().Year, x, siteMonthlyOutputFile);
                        });
        
    }

    var siteSetYear = new Dictionary<int, List<YearlyAverageSiteTemps>>();
    {
        var year = earliestYearInSiteSet;
        while (year <= latestYearInSiteSet)
        {
            siteSetYear.Add(year, new List<YearlyAverageSiteTemps>());
            year++;
        }
    }

    foreach (var site in siteSet)
    {
        var rows = File.ReadAllLines(@$"{folderName}\{site} {averageDailyFileNameSuffix}.csv");
        var records = rows.Take(new Range(1, rows.Length));
        foreach (var record in records)
        {
            var data = record.Split(',');
            var year = int.Parse(data[0]);
            double? min = string.IsNullOrWhiteSpace(data[1]) ? null : double.Parse(data[1]);
            var numberOfRecordsMin = int.Parse(data[2]);

            double? max = string.IsNullOrWhiteSpace(data[3]) ? null : double.Parse(data[3]);
            var numberOfRecordsMax = int.Parse(data[4]);

            if (numberOfRecordsMin >= minNumberOfDailyRecordsForTheYear && numberOfRecordsMax >= minNumberOfDailyRecordsForTheYear)
            {
                siteSetYear[year].Add(new YearlyAverageSiteTemps { Year = year, Min = min, Max = max, SiteName = site });
            }
        }
    }

    var fileName = @$"{folderName}\{location.Name} {averageDailyFileNameSuffix}.csv";
    WriteLocationYearAverage(siteSetYear, fileName);

    Directory.CreateDirectory("Data");
    fileName = @$"Data\{location.PrimarySite}.csv";
    WriteLocationYearAverage(siteSetYear, fileName);
}

void WriteLocationYearAverage(Dictionary<int, List<YearlyAverageSiteTemps>> siteSetYear, string fileName)
{
    using StreamWriter siteSetFile = new(fileName);
    siteSetFile.WriteLine($"Year,YearAverageMin,YearAverageMax,NumberOfSites,Sites");

    foreach (var year in siteSetYear.Keys)
    {
        if (siteSetYear[year].Any())
        {
            var min = siteSetYear[year].Where(x => x.Min != null).Average(x => x.Min);
            var max = siteSetYear[year].Where(x => x.Max != null).Average(x => x.Max);
            siteSetFile.WriteLine($"{year},{min},{max},{siteSetYear[year].Count},\"=\"\"{string.Join(';', siteSetYear[year].Select(x => x.SiteName))}\"\"\"");
        }
        else
        {
            siteSetFile.WriteLine($"{year},,,0,");
        }
    }
}

void WriteYearData(int year, List<RawDataRecord> rawDataRecord, StreamWriter siteFile)
{
    // Temp values are recorded as tenths of degrees C in ACORN-SAT. Divide by 10 to get them into degrees C.
    // E.g., 222 = 22.2 degrees C
    var dailyMin = rawDataRecord
        .Where(x => x.Min != null)
        .Select(x => x.Min / 10.0)
        .ToList();

    var dailyMax = rawDataRecord
        .Where(x => x.Max != null)
        .Select(x => x.Max / 10.0)
        .ToList();

    siteFile.WriteLine($"{year},{dailyMin.Average()},{dailyMin.Count},{dailyMax.Average()},{dailyMax.Count}");
}

void WriteMonthData(int year, List<RawDataRecord> rawDataRecord, StreamWriter siteFile)
{
    for (int month = 1; month <= 12; month++)
    {
        var monthData = rawDataRecord.Where(x => x.Month == month).ToList();

        // Temp values are recorded as tenths of degrees C in ACORN-SAT. Divide by 10 to get them into degrees C.
        // E.g., 222 = 22.2 degrees C
        var dailyMin = monthData
            .Where(x => x.Min != null)
            .Select(x => x.Min / 10.0)
            .ToList();

        var dailyMax = monthData
            .Where(x => x.Max != null)
            .Select(x => x.Max / 10.0)
            .ToList();

        siteFile.WriteLine($"{year},{month},{dailyMin.Average()},{dailyMin.Count},{dailyMax.Average()},{dailyMax.Count}");
    }
}

List<RawDataRecord> ReadRawDataFile(string site)
{
    var siteFilePath = @$"raw-data\hqnew{site}.txt";
    var rawData = File.ReadAllLines(siteFilePath);
    var rawDataRecords = new List<RawDataRecord>();
    foreach (var record in rawData)
    {
        var year = int.Parse(record.Substring(6, 4));
        var month = int.Parse(record.Substring(10, 2));
        var day = int.Parse(record.Substring(12, 2));
        var temps = record.Substring(13);
        var tempGroups = _tempsRegEx.Match(temps).Groups;

        // Some recordings don't have a value for min or max. In that case the entry will be -999. Will make those values null
        int? maxTemp = tempGroups[1].Value == "-999" ? null : int.Parse(tempGroups[1].Value);
        int? minTemp = tempGroups[2].Value == "-999" ? null : int.Parse(tempGroups[2].Value);

        rawDataRecords.Add(new RawDataRecord
        {
            Day = day,
            Month = month,
            Year = year,
            Min = minTemp,
            Max = maxTemp,
        });
    }
    return rawDataRecords;
}