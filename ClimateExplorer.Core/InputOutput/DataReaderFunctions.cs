namespace ClimateExplorer.Core.InputOutput;

using System.IO.Compression;
using System.Text.RegularExpressions;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public static class DataReaderFunctions
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Regex> DataRowRegExCache = new();

    private static readonly Dictionary<string, short> MonthNamesToNumeric = new()
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

    public static async Task<List<DataRecord>> GetDataRecords(
        MeasurementDefinition measurementDefinition,
        List<DataFileFilterAndAdjustment>? dataFileFilterAndAdjustments,
        string datasetsFolder = "Datasets")
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

        var regEx = DataRowRegExCache.GetOrAdd(measurementDefinition.DataRowRegEx!, static pattern => new Regex(pattern, RegexOptions.Compiled));

        var records = new List<DataRecord>();

        // Each file's records are already unique (ProcessDataFile skips out-of-order/duplicate rows), so
        // overlap only needs checking when more than one file contributes.
        var seenDates = dataFileFilterAndAdjustments.Count > 1 ? new HashSet<(short Year, short? Month, short? Day)>() : null;
        var dataFileSource = measurementDefinition.DataFileSource
            ?? throw new InvalidOperationException("Every measurement definition must have an explicit data file source.");
        foreach (var dataFileDefinition in dataFileFilterAndAdjustments)
        {
            var fileRecords = await ReadDataFile(dataFileSource, regEx, measurementDefinition.NullValue!, measurementDefinition.DataResolution, dataFileDefinition.Id, datasetsFolder, dataFileDefinition.StartDate, dataFileDefinition.EndDate);

            foreach (var dataRecord in fileRecords)
            {
                // Adjust based on the measurement definition (how the data is stored on file vs the unit of measure in the measurement definition).
                if (measurementDefinition.ValueAdjustment != null)
                {
                    dataRecord.Value = dataRecord.Value / measurementDefinition.ValueAdjustment.Value;
                }

                if (seenDates != null && !seenDates.Add((dataRecord.Year, dataRecord.Month, dataRecord.Day)))
                {
                    throw new Exception($"Record {dataRecord.Year}-{dataRecord.Month}-{dataRecord.Day} already exists in the collection");
                }

                records.Add(dataRecord);
            }
        }

        return records;
    }

    /// <summary>
    /// Parses the lines of a data file into records in ascending date order, inserting null-valued
    /// records for any gaps. Out-of-order or duplicate rows are skipped, so each date appears at most once.
    /// </summary>
    public static List<DataRecord> ProcessDataFile(
        string[]? linesOfFile,
        Regex regEx,
        string nullValue,
        DataResolution dataResolution,
        string station,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
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

        var dataRecords = new List<DataRecord>(lines.Length);

        var initialDataIndex = GetStartIndex(regEx, lines, station);

        var firstValidLine = regEx.Match(lines[initialDataIndex]);

        // Look up group numbers once rather than resolving group names on every line
        var yearGroup = regEx.GroupNumberFromName("year");
        var monthGroup = regEx.GroupNumberFromName("month");
        var dayGroup = regEx.GroupNumberFromName("day");
        var valueGroup = regEx.GroupNumberFromName("value");

        var startYear = short.Parse(firstValidLine.Groups[yearGroup].ValueSpan);
        var startMonth = GetMonthValue(firstValidLine.Groups[monthGroup]);

        short startDay = 1;
        if (dataResolution == DataResolution.Daily)
        {
            startDay = short.Parse(firstValidLine.Groups[dayGroup].ValueSpan);
        }

        var date = new DateOnly(startYear, startMonth, startDay);
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

            var groups = match.Groups;
            var year = short.Parse(groups[yearGroup].ValueSpan);
            var month = GetMonthValue(groups[monthGroup]);
            short day = 1;
            if (dataResolution == DataResolution.Daily)
            {
                day = short.Parse(groups[dayGroup].ValueSpan);
            }

            var filterDate = new DateOnly(year, month, day);
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

            var recordDate = new DateOnly(year, month, day);
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
                    dataRecords.Add(new DataRecord(date, null));
                    date = date.AddDays(1);
                }
                else if (dataResolution == DataResolution.Monthly)
                {
                    dataRecords.Add(new DataRecord((short)date.Year, (short)date.Month, null, null));
                    date = date.AddMonths(1);
                }
            }

            var value = ParseValue(groups[valueGroup].ValueSpan, nullValue);

            previousDate = recordDate;
            if (dataResolution == DataResolution.Daily)
            {
                dataRecords.Add(new DataRecord(year, month, day, value));
                date = date.AddDays(1);
            }
            else if (dataResolution == DataResolution.Monthly)
            {
                dataRecords.Add(new DataRecord(year, month, null, value));
                date = date.AddMonths(1);
            }
        }

        return dataRecords;
    }

    public static async Task<string[]?> GetLinesInDataFileSource(
        DataFileSourceDefinition dataFileSource,
        string station,
        string datasetsFolder = "Datasets")
    {
        ArgumentNullException.ThrowIfNull(dataFileSource);

        var sourceFilePath = ResolveSourceFilePath(dataFileSource.FilePathFormat, station, datasetsFolder);
        if (!File.Exists(sourceFilePath))
        {
            return null;
        }

        if (dataFileSource.ArchiveEntryPathFormat == null)
        {
            return await File.ReadAllLinesAsync(sourceFilePath);
        }

        var archiveEntryPath = ResolveArchiveEntryPath(dataFileSource.ArchiveEntryPathFormat, station);
        return ReadLinesFromZipFileEntry(sourceFilePath, archiveEntryPath);
    }

    private static async Task<List<DataRecord>> ReadDataFile(
        DataFileSourceDefinition dataFileSource,
        Regex regEx,
        string nullValue,
        DataResolution dataResolution,
        string station,
        string datasetsFolder,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var lines = await GetLinesInDataFileSource(dataFileSource, station, datasetsFolder);

        return ProcessDataFile(lines, regEx, nullValue, dataResolution, station, startDate, endDate);
    }

    private static double? ParseValue(ReadOnlySpan<char> valueSpan, string nullValue)
    {
        return valueSpan.IsWhiteSpace() || valueSpan.SequenceEqual(nullValue) ? null : double.Parse(valueSpan);
    }

    private static short GetMonthValue(Group monthGroup)
    {
        if (short.TryParse(monthGroup.ValueSpan, out short monthValue))
        {
            return monthValue;
        }

        if (MonthNamesToNumeric.TryGetValue(monthGroup.Value, out monthValue))
        {
            return monthValue;
        }

        throw new FormatException($"Month field (value is '{monthGroup.Value}') is an unrecognised format");
    }

    private static List<DataRecord> ProcessYearlyData(string[]? linesOfFile, Regex regEx, string nullValue, DataResolution dataResolution, string station)
    {
        var lines = linesOfFile;

        if (lines == null)
        {
            // We couldn't find the data in any of the expected locations
            return [];
        }

        var dataRecords = new List<DataRecord>();

        var initialDataIndex = GetStartIndex(regEx, lines, station);

        var firstValidLine = regEx.Match(lines[initialDataIndex]);

        var startYear = short.Parse(firstValidLine.Groups["year"].Value);

        // Yearly data represents data from the whole year, so use the last day of the year as the "date"
        var date = new DateOnly(startYear, 12, 31);
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

            var recordDate = new DateOnly(year, 12, 31);
            if (recordDate <= previousDate)
            {
                Console.Error.WriteLine($"Date of current record ({recordDate}) is earlier than or equal to the previous date ({previousDate}). The file is not ordered by date properly and/or there are duplicate records. Will skip this record.");
                continue;
            }

            while (recordDate > date)
            {
                dataRecords.Add(new DataRecord(date, null));
                date = date.AddYears(1);
            }

            dataRecords.Add(new DataRecord(year, 12, 31, ParseValue(match.Groups["value"].ValueSpan, nullValue)));

            previousDate = recordDate;
            date = date.AddYears(1);
        }

        return dataRecords;
    }

    private static string ResolveSourceFilePath(string pathFormat, string station, string datasetsFolder)
    {
        var relativePath = ResolvePathFormat(pathFormat, station);
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"Data source path '{relativePath}' must be relative to the datasets folder.");
        }

        var rootPath = Path.GetFullPath(datasetsFolder);
        var sourceFilePath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        var rootPathWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        if (!sourceFilePath.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Data source path '{relativePath}' resolves outside the datasets folder.");
        }

        return sourceFilePath;
    }

    private static string ResolveArchiveEntryPath(string pathFormat, string station)
    {
        var entryPath = ResolvePathFormat(pathFormat, station).Replace('\\', '/');
        var pathSegments = entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (Path.IsPathRooted(entryPath) || pathSegments.Any(x => x is "." or ".."))
        {
            throw new InvalidOperationException($"Archive entry path '{entryPath}' must be a relative path within the archive.");
        }

        return entryPath;
    }

    private static string ResolvePathFormat(string pathFormat, string station)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathFormat);

        var resolvedPath = pathFormat.Replace("[station]", station, StringComparison.Ordinal);
        if (resolvedPath.Contains('[') || resolvedPath.Contains(']'))
        {
            throw new InvalidOperationException($"Data source path '{pathFormat}' contains an unresolved placeholder.");
        }

        return resolvedPath;
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

        using StreamReader sr = new(siteFileEntry.Open());

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
