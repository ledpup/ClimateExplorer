using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

Regex _tempsRegEx = new Regex(@"\s+(-*\d+)\s+(-*\d+)");

//BuildLocationsFromReferenceData();
var locations = Location.GetLocations("locations.json");

var minNumberOfDailyRecordsForTheYear = args.Length > 0 ? string.IsNullOrWhiteSpace(args[0]) ? 355 : int.Parse(args[0]) : 355;
var averageDailyFileNameSuffix = $"yearly average from daily records";

var minNumberOfDailyRecordsForTheMonth = args.Length > 1 ? string.IsNullOrWhiteSpace(args[1]) ? 20 : int.Parse(args[0]) : 20;
var averageMonthlyFileNameSuffix = $"monthly average from daily records";

foreach (var location in locations)
{
    var siteSet = location.Sites;

    var folderName = $@"Output\{location.Name.Replace(" ", "_")}";
    Directory.CreateDirectory(folderName);

    var earliestYearInSiteSet = 3000;
    var latestYearInSiteSet = 0;

    
    var missingFileSites = siteSet.Where(x => !File.Exists(@$"raw-data\hqnew{x}.txt"));
    
    if (missingFileSites.Any())
    {
        var missingFileNames = missingFileSites.Select(x => @$"raw-data\hqnew{x}.txt");
        Console.WriteLine($"Not all site files exist for {location.Name}. The missing files are: {string.Join(", ", missingFileNames)}. Will remove these sites from the set continue processing {location.Name}.");
    }

    siteSet = siteSet.Where(x => File.Exists(@$"raw-data\hqnew{x}.txt")).ToList();

    foreach (var site in siteSet)
    {
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

        using StreamWriter siteDailyOutputFile = new(@$"{folderName}\{site} unadjusted {averageDailyFileNameSuffix}.csv");
        siteDailyOutputFile.WriteLine($"Year,YearAverageMin,DaysOfDataMin,YearAverageMax,DaysOfDataMax");

        using StreamWriter siteMonthlyOutputFile = new(@$"{folderName}\{site} unadjusted {averageMonthlyFileNameSuffix}.csv");
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

    CreateLocationYearlyAverage(earliestYearInSiteSet, latestYearInSiteSet, "unadjusted", folderName, location.Name, location.Id, siteSet);
    
    {
        var adjustDailyTemp = CreatedAdjustedAverages(location, folderName, averageDailyFileNameSuffix);

        using StreamWriter siteDailyOutputFile = new(@$"{folderName}\{location.Id} adjusted {averageDailyFileNameSuffix}.csv");
        siteDailyOutputFile.WriteLine($"Year,YearAverageMin,DaysOfDataMin,YearAverageMax,DaysOfDataMax");

        adjustDailyTemp.GroupBy(x => x.Year)
                    .Select(x => x.ToList())
                    .ToList()
                    .ForEach(x =>
                    {
                        WriteYearData(x.First().Year, x, siteDailyOutputFile);
                    });

        var earliestYear = adjustDailyTemp.Min(x => x.Year);
        var latestYear = adjustDailyTemp.Max(x => x.Year);

        siteDailyOutputFile.Close();

        CreateLocationYearlyAverage(earliestYear, latestYear, "adjusted", folderName, location.Name, location.Id, new List<string> { location.Id.ToString() });
    }
}

void CreateLocationYearlyAverage(int earliestYear, int latestYear, string dataType, string folderName, string locationName, Guid locationId, List<string> siteSet)
{
    var siteSetYear = new Dictionary<int, List<YearlyAverageSiteTemps>>();
    {
        var year = earliestYear;
        while (year <= latestYear)
        {
            siteSetYear.Add(year, new List<YearlyAverageSiteTemps>());
            year++;
        }
    }

    foreach (var site in siteSet)
    {
        var rows = File.ReadAllLines(@$"{folderName}\{site} {dataType} {averageDailyFileNameSuffix}.csv");
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

    var fileName = @$"{folderName}\{locationName} {dataType} {averageDailyFileNameSuffix}.csv";
    WriteLocationYearAverage(siteSetYear, fileName);

    Directory.CreateDirectory(dataType);
    fileName = @$"{dataType}\{locationId}.csv";
    WriteLocationYearAverage(siteSetYear, fileName);
}

List<TemperatureRecord> CreatedAdjustedAverages(Location location, string folderName, string averageDailyFileNameSuffix)
{
    var maximumsFilePath = string.Empty;
    var minimumsFilePath = string.Empty;
    foreach (var site in location.Sites)
    {
        maximumsFilePath = @$"daily_tmax\tmax.{site}.daily.csv";
        minimumsFilePath = @$"daily_tmin\tmin.{site}.daily.csv";

        if (File.Exists(maximumsFilePath) && File.Exists(minimumsFilePath))
        {
            break;
        }
    }

    var maximums = File.ReadAllLines(maximumsFilePath);
    var minimums = File.ReadAllLines(minimumsFilePath);

    if (maximums.Length != minimums.Length)
    {
        throw new Exception("Max and min files are not the same length");
    }

    var adjustedTemperatureRecord = new List<TemperatureRecord>();
    for (var i = 2; i < maximums.Length; i++)
    {
        var splitMax = maximums[i].Split(',');
        var splitMin = minimums[i].Split(',');

        var date = DateTime.Parse(splitMax[0]);
        var minDate = DateTime.Parse(splitMin[0]);

        if (date != minDate)
        {
            throw new Exception("Max and min dates do not match");
        }

        adjustedTemperatureRecord.Add(
            new TemperatureRecord
            {
                Year = (short)date.Year,
                Month = (short)date.Month,
                Day = (short)date.Day,
                Max = string.IsNullOrWhiteSpace(splitMax[1]) ? null : float.Parse(splitMax[1]),
                Min = string.IsNullOrWhiteSpace(splitMin[1]) ? null : float.Parse(splitMin[1]),
            });
    }

    return adjustedTemperatureRecord;
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

void WriteYearData(int year, List<TemperatureRecord> rawDataRecord, StreamWriter siteFile)
{

    var dailyMin = rawDataRecord
        .Where(x => x.Min != null)
        .Select(x => x.Min)
        .ToList();

    var dailyMax = rawDataRecord
        .Where(x => x.Max != null)
        .Select(x => x.Max)
        .ToList();

    siteFile.WriteLine($"{year},{dailyMin.Average()},{dailyMin.Count},{dailyMax.Average()},{dailyMax.Count}");
}

void WriteMonthData(int year, List<TemperatureRecord> rawDataRecord, StreamWriter siteFile)
{
    for (int month = 1; month <= 12; month++)
    {
        var monthData = rawDataRecord.Where(x => x.Month == month).ToList();

        var dailyMin = monthData
            .Where(x => x.Min != null)
            .Select(x => x.Min)
            .ToList();

        var dailyMax = monthData
            .Where(x => x.Max != null)
            .Select(x => x.Max)
            .ToList();

        siteFile.WriteLine($"{year},{month},{dailyMin.Average()},{dailyMin.Count},{dailyMax.Average()},{dailyMax.Count}");
    }
}

List<TemperatureRecord> ReadRawDataFile(string site)
{
    var siteFilePath = @$"raw-data\hqnew{site}.txt";
    var rawData = File.ReadAllLines(siteFilePath);
    var rawDataRecords = new List<TemperatureRecord>();
    foreach (var record in rawData)
    {
        var year = short.Parse(record.Substring(6, 4));
        var month = short.Parse(record.Substring(10, 2));
        var day = short.Parse(record.Substring(12, 2));
        var temps = record.Substring(13);
        var tempGroups = _tempsRegEx.Match(temps).Groups;

        // Some recordings don't have a value for min or max. In that case the entry will be -999. Will make those values null
        // Temp values are recorded as tenths of degrees C in ACORN-SAT raw data. Divide by 10 to get them into degrees C.
        // E.g., 222 = 22.2 degrees C
        float? max = tempGroups[1].Value == "-999" ? null : float.Parse(tempGroups[1].Value) / 10;
        float? min = tempGroups[2].Value == "-999" ? null : float.Parse(tempGroups[2].Value) / 10;

        rawDataRecords.Add(new TemperatureRecord
        {
            Day = day,
            Month = month,
            Year = year,
            Min = min,
            Max = max,
        });
    }
    return rawDataRecords;
}


void BuildLocationsFromReferenceData()
{
    var locations = new List<Location>();

    var locationRowData = File.ReadAllLines(@"ReferenceData\Locations.csv");
    foreach (var row in locationRowData)
    {
        var splitRow = row.Split(',');
        var location = new Location { Id = Guid.NewGuid(), Name = splitRow[0] };
        location.Sites.Add(splitRow[1]);
        if (splitRow.Length > 2 && !string.IsNullOrWhiteSpace(splitRow[2]))
        {
            location.Sites.Add(splitRow[2]);
        }
        locations.Add(location);
    }

    var primarySites = File.ReadAllLines(@"ReferenceData\primarysites.txt");

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

        var location = locations.Single(x => x.Sites.Contains(primarySite));
        siteSets[primarySite].ForEach(x =>
        {
            if (!location.Sites.Contains(x))
            {
                location.Sites.Add(x);
            }
        }
        );
    }

    var moreLocationData = File.ReadAllLines(@"ReferenceData\acorn_sat_v2.1.0_stations.csv");

    for (var i = 1; i < moreLocationData.Length; i++)
    {
        var splitRow = moreLocationData[i].Split(',');
        var id = splitRow[0].PadLeft(6, '0');

        var location = locations.Single(x => x.Sites.Contains(id));

        if (location.Name != splitRow[1])
        {
            Console.WriteLine($"Location name mismatch. '{location.Name}' not equal to '{splitRow[1]}'");
        }

        location.Coordinates = new Coordinates
        {
            Latitude = float.Parse(splitRow[2]),
            Longitude = float.Parse(splitRow[3]),
            Elevation = float.Parse(splitRow[4]),
        };
    }

    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText("locations.json", JsonSerializer.Serialize(locations, options));
}