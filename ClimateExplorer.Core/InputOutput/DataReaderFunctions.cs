namespace ClimateExplorer.Core.InputOutput;

using ClimateExplorer.Core.Model;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using static ClimateExplorer.Core.Enums;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
public static class DataReaderFunctions
{
    private static readonly Dictionary<string, short> MonthNamesToNumeric = new ()
    {
        { "jan", 1 },
        { "feb", 2 },
        { "mar", 3 },
        { "apr", 4 },
        { "may", 5 },
        { "jun", 6 },
        { "jul", 7 },
        { "aug", 8 },
        { "sep", 9 },
        { "oct", 10 },
        { "nov", 11 },
        { "dec", 12 },
    };

    public static async Task<DataSet> GetDataSet(MeasurementDefinition measurementDefinition, List<DataFileFilterAndAdjustment> dataFileDefinitions)
    {
        var records = await GetDataRecords(measurementDefinition, dataFileDefinitions);

        short? year = null;
        if (records != null && records.Any())
        {
            var firstYear = records.First().Year;
            year = records.All(x => x.Year == firstYear) ? firstYear : null;
        }

        var dataSet = new DataSet
        {
            Resolution = measurementDefinition.DataResolution,
            MeasurementDefinition = measurementDefinition.ToViewModel(),
            Year = year,
            DataRecords = records!,
        };

        return dataSet;
    }

    public static async Task<List<DataRecord>> GetDataRecords(
        MeasurementDefinition measurementDefinition,
        List<DataFileFilterAndAdjustment>? dataFileFilterAndAdjustments)
    {
        if (dataFileFilterAndAdjustments == null)
        {
            dataFileFilterAndAdjustments =
            [
                new DataFileFilterAndAdjustment
                {
                    Id = string.Empty,
                }

            ];
        }

        var regEx = new Regex(measurementDefinition.DataRowRegEx!);

        var records = new Dictionary<string, DataRecord>();
        foreach (var dataFileDefinition in dataFileFilterAndAdjustments)
        {
            var filePath = measurementDefinition.FolderName + @"\" + measurementDefinition.FileNameFormat!.Replace("[station]", dataFileDefinition.Id);
            var fileRecords = await ReadDataFile(filePath, regEx, measurementDefinition.NullValue!, measurementDefinition.DataResolution, dataFileDefinition.Id, dataFileDefinition.StartDate, dataFileDefinition.EndDate);
            var values = fileRecords.Values.ToList();

            // Adjust based on the measurement definition (how the data is stored on file vs the unit of measure in the measurement definition).
            if (measurementDefinition.ValueAdjustment != null)
            {
                values.ForEach(x => x.Value = x.Value / measurementDefinition.ValueAdjustment.Value);
            }

            // Add to full record set
            if (fileRecords != null)
            {
                foreach (var dataRecord in fileRecords)
                {
                    if (!records.ContainsKey(dataRecord.Key))
                    {
                        records.Add(dataRecord.Key, dataRecord.Value);
                    }
                    else
                    {
                        throw new Exception($"Key {dataRecord.Key} already exists in the collection");
                    }
                }
            }
        }

        return records.Values.ToList();
    }

    public static Dictionary<string, DataRecord> ProcessDataFile(
        string[]? linesOfFile,
        Regex regEx,
        string nullValue,
        DataResolution dataResolution,
        string station,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        switch (dataResolution)
        {
            case DataResolution.Yearly:
                return ProcessYearlyData(linesOfFile, regEx, nullValue, dataResolution, station);
            case DataResolution.Weekly:
                throw new NotImplementedException("Supported data resolution is currently only daily or monthly.");
        }

        var lines = linesOfFile;

        if (lines == null)
        {
            // We couldn't find the data in any of the expected locations
            return [];
        }

        var dataRecords = new Dictionary<string, DataRecord>();

        var initialDataIndex = GetStartIndex(regEx, lines, station);

        var firstValidLine = regEx.Match(lines[initialDataIndex]);

        var startYear = short.Parse(firstValidLine.Groups["year"].Value);
        var startMonth = GetMonthValue(firstValidLine);

        short startDay = 1;
        if (dataResolution == DataResolution.Daily)
        {
            startDay = short.Parse(firstValidLine.Groups["day"].Value);
        }

        var date = new DateTime(startYear, startMonth, startDay);
        var previousDate = date.AddDays(-1);
        var resetDate = false;
        foreach (var line in lines)
        {
            var match = regEx.Match(line);

            // Is the line we've moved to a line that fits as a DataRecord? If not, skip it
            if (!match.Success)
            {
                continue;
            }

            var year = short.Parse(match.Groups["year"].Value);
            var month = GetMonthValue(match);
            short day = 1;
            if (dataResolution == DataResolution.Daily)
            {
                day = short.Parse(match.Groups["day"].Value);
            }

            var filterDate = new DateTime(year, month, day);
            if (startDate.HasValue && filterDate < startDate.Value)
            {
                resetDate = true;
                continue;
            }
            else if (endDate.HasValue && filterDate > endDate.Value)
            {
                break;
            }

            if (resetDate)
            {
                date = filterDate;
                resetDate = false;
            }

            var recordDate = new DateTime(year, month, day);
            if (recordDate <= previousDate)
            {
                Console.Error.WriteLine($"Date of current record ({recordDate}) is earlier than or equal to the previous date ({previousDate}). The file is not ordered by date properly and/or there are duplicate records. Will skip this record.");
                continue;
            }

            // If the record date is beyond the date we're expecting, add in the gaps as null and then go to the next day/month
            while (recordDate > date)
            {
                if (dataResolution == DataResolution.Daily)
                {
                    var record = new DataRecord(date, null);
                    dataRecords.Add(record.Key!, record);
                    date = date.AddDays(1);
                }
                else if (dataResolution == DataResolution.Monthly)
                {
                    var record = new DataRecord((short)date.Year, (short)date.Month, null, null);
                    dataRecords.Add(record.Key!, record);
                    date = date.AddMonths(1);
                }
            }

            var valueString = match.Groups["value"].Value;
            double? value = string.IsNullOrWhiteSpace(valueString) || valueString == nullValue ? null : double.Parse(valueString);

            previousDate = recordDate;
            if (dataResolution == DataResolution.Daily)
            {
                var record = new DataRecord(year, month, day, value);
                dataRecords.Add(record.Key!, record);
                date = date.AddDays(1);
            }
            else if (dataResolution == DataResolution.Monthly)
            {
                var record = new DataRecord(year, month, null, value);
                dataRecords.Add(record.Key!, record);
                date = date.AddMonths(1);
            }
        }

        return dataRecords;
    }

    public static async Task<string[]> GetLinesInDataFileWithCascade(string dataFilePath)
    {
        string[]? lines = TryGetDataFromDatasetZipFile(dataFilePath);

        if (lines == null)
        {
            lines = TryGetDataFromSingleEntryZipFile(dataFilePath);
        }

        if (lines == null)
        {
            lines = await TryGetDataFromUncompressedSingleFile(dataFilePath);
        }

        return lines!;
    }

    private static async Task<Dictionary<string, DataRecord>> ReadDataFile(
        string pathAndFile,
        Regex regEx,
        string nullValue,
        DataResolution dataResolution,
        string station,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        string[]? lines = await GetLinesInDataFileWithCascade(pathAndFile);

        return ProcessDataFile(lines, regEx, nullValue, dataResolution, station, startDate, endDate);
    }

    private static short GetMonthValue(Match match)
    {
        var isMonthParsed = short.TryParse(match.Groups["month"].Value, out short monthValue);
        if (!isMonthParsed)
        {
            if (MonthNamesToNumeric.ContainsKey(match.Groups["month"].Value))
            {
                monthValue = MonthNamesToNumeric[match.Groups["month"].Value];
            }
            else
            {
                throw new FormatException($"Month field (value is '{match.Groups["month"].Value}') is an unrecognised format");
            }
        }

        return monthValue;
    }

    private static Dictionary<string, DataRecord> ProcessYearlyData(string[]? linesOfFile, Regex regEx, string nullValue, DataResolution dataResolution, string station)
    {
        var lines = linesOfFile;

        if (lines == null)
        {
            // We couldn't find the data in any of the expected locations
            return [];
        }

        var dataRecords = new Dictionary<string, DataRecord>();

        var initialDataIndex = GetStartIndex(regEx, lines, station);

        var firstValidLine = regEx.Match(lines[initialDataIndex]);

        var startYear = short.Parse(firstValidLine.Groups["year"].Value);

        // Yearly data represents data from the whole year, so use the last day of the year as the "date"
        var date = new DateTime(startYear, 12, 31);
        var previousDate = date.AddYears(-1);
        foreach (var line in lines)
        {
            var match = regEx.Match(line);

            // Is the line we've moved to a line that fits as a DataRecord? If not, skip it
            if (!Match(station, match))
            {
                continue;
            }

            var year = short.Parse(match.Groups["year"].Value);

            var recordDate = new DateTime(year, 12, 31);
            if (recordDate <= previousDate)
            {
                Console.Error.WriteLine($"Date of current record ({recordDate}) is earlier than or equal to the previous date ({previousDate}). The file is not ordered by date properly and/or there are duplicate records. Will skip this record.");
                continue;
            }

            while (recordDate > date)
            {
                var record = new DataRecord(date, null);
                dataRecords.Add(record.Key!, record);
                date = date.AddYears(1);
            }

            {
                var valueString = match.Groups["value"].Value;
                double? value = string.IsNullOrWhiteSpace(valueString) || valueString == nullValue ? null : double.Parse(valueString);

                var record = new DataRecord(year, 12, 31, value);
                dataRecords.Add(record.Key!, record);
            }

            previousDate = recordDate;
            date = date.AddYears(1);
        }

        return dataRecords;
    }

    private static async Task<string[]?> TryGetDataFromUncompressedSingleFile(string siteFilePath)
    {
        if (File.Exists(siteFilePath))
        {
            return await File.ReadAllLinesAsync(siteFilePath);
        }

        return null;
    }

    private static string[]? TryGetDataFromDatasetZipFile(string filePath)
    {
        string[] pathComponents =
            filePath.Split(
                new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

        var shallowestFolderName = pathComponents.First();

        var datasetName = shallowestFolderName;

        string zipPath = Path.Combine("Datasets", datasetName + ".zip");

        if (!File.Exists(zipPath))
        {
            return null;
        }

        var zipEntryPath = string.Join('/', pathComponents.Skip(1));

        Debug.WriteLine("Reading from zip " + zipPath);
        return ReadLinesFromZipFileEntry(zipPath, zipEntryPath);
    }

    private static string[]? TryGetDataFromSingleEntryZipFile(string filePath)
    {
        var zipPath = Path.Combine("Datasets", Path.ChangeExtension(filePath, ".zip"));

        if (!File.Exists(zipPath))
        {
            return null;
        }

        return ReadLinesFromZipFileEntry(zipPath, Path.GetFileName(filePath));
    }

    private static string[]? ReadLinesFromZipFileEntry(string zipFilename, string zipEntryFilename)
    {
        using FileStream zipFileStream = new FileStream(zipFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
        using ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read);
        ZipArchiveEntry? siteFileEntry = archive.GetEntry(zipEntryFilename);

        if (siteFileEntry == null)
        {
            return null;
        }

        using StreamReader sr = new (siteFileEntry.Open());

        // This could probably be optimized
        var lineList = new List<string>();

        while (true)
        {
            var line = sr.ReadLine();

            if (line != null)
            {
                lineList.Add(line);
            }
            else
            {
                break;
            }
        }

        return lineList.ToArray();
    }

    private static int GetStartIndex(Regex regEx, string[] dataRows, string station)
    {
        var index = 0;
        Match match = regEx.Match(dataRows[index]);
        while (!Match(station, match))
        {
            index++;
            if (index >= dataRows.Length)
            {
                throw new FileLoadException("None of the data in the input file fits the regular expression.");
            }

            match = regEx.Match(dataRows[index]);
        }

        return index;
    }

    private static bool Match(string station, Match match)
    {
        if (match.Success)
        {
            if (match.Groups.ContainsKey("station"))
            {
                if (match.Groups["station"].Value == station || string.IsNullOrEmpty(station))
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }
}
