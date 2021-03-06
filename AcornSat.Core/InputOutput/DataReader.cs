using AcornSat.Core.Model;
using AcornSat.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.Core.InputOutput;

public static class DataReader
{
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
            DataRecords = records,
        };

        return dataSet;
    }

    public static async Task<List<DataRecord>> GetDataRecords(
        MeasurementDefinition measurementDefinition, 
        List<DataFileFilterAndAdjustment> dataFileFilterAndAdjustments)
    {
        if (dataFileFilterAndAdjustments == null)
        {
            dataFileFilterAndAdjustments = new List<DataFileFilterAndAdjustment>
            {
                new DataFileFilterAndAdjustment
                {
                    ExternalStationCode = string.Empty
                }
            };
        }

        var regEx = new Regex(measurementDefinition.DataRowRegEx);

        var records = new Dictionary<string, DataRecord>();
        foreach (var dataFileDefinition in dataFileFilterAndAdjustments)
        {
            var filePath = measurementDefinition.FolderName + @"\" + measurementDefinition.FileNameFormat.Replace("[station]", dataFileDefinition.ExternalStationCode);
            var fileRecords = await ReadDataFile(filePath, regEx, measurementDefinition.NullValue, measurementDefinition.DataResolution, dataFileDefinition.StartDate, dataFileDefinition.EndDate);

            // Apply any adjustment
            var values = fileRecords.Values.ToList();
            if (dataFileDefinition.ValueAdjustment != null)
            {
                values.ForEach(x =>
                {
                    if (x.Value.HasValue)
                    {
                        x.Value += dataFileDefinition.ValueAdjustment;
                    }
                });
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

    static async Task<Dictionary<string, DataRecord>> ReadDataFile(
        string pathAndFile,
        Regex regEx,
        string nullValue,
        DataResolution dataResolution,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        string[]? lines = await GetLinesInDataFileWithCascade(pathAndFile);

        return await ProcessDataFile(lines, regEx, nullValue, dataResolution, startDate, endDate);
    }

    public static async Task<Dictionary<string, DataRecord>> ProcessDataFile(
    string[]? linesOfFile,
    Regex regEx,
    string nullValue,
    DataResolution dataResolution,
    DateTime? startDate = null,
    DateTime? endDate = null)
    {
        switch (dataResolution)
        {
            case DataResolution.Yearly:
            case DataResolution.Weekly:
                throw new NotImplementedException("Supported data resolution is currently only daily or monthly.");
        }

        var lines = linesOfFile;

        if (lines == null)
        {
            // We couldn't find the data in any of the expected locations
            return new Dictionary<string, DataRecord>();
        }

        var dataRecords = new Dictionary<string, DataRecord>();

        var initialDataIndex = GetStartIndex(regEx, lines);

        var firstValidLine = regEx.Match(lines[initialDataIndex]);

        var startYear = short.Parse(firstValidLine.Groups["year"].Value);
        var startMonth = short.Parse(firstValidLine.Groups["month"].Value);
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
            var month = short.Parse(match.Groups["month"].Value);
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
                    dataRecords.Add(record.Key, record);
                    date = date.AddDays(1);
                }
                else if (dataResolution == DataResolution.Monthly)
                {
                    var record = new DataRecord((short)date.Year, (short)date.Month, null, null);
                    dataRecords.Add(record.Key, record);
                    date = date.AddMonths(1);
                }
            }

            var valueString = match.Groups["value"].Value;
            float? value = string.IsNullOrWhiteSpace(valueString) || valueString == nullValue ? null : float.Parse(valueString);

            previousDate = recordDate;
            if (dataResolution == DataResolution.Daily)
            {
                var record = new DataRecord(year, month, day, value);
                dataRecords.Add(record.Key, record);
                date = date.AddDays(1);
            }
            else if (dataResolution == DataResolution.Monthly)
            {
                var record = new DataRecord(year, month, null, value);
                dataRecords.Add(record.Key, record);
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

        return lines;
    }

    static async Task<string[]?> TryGetDataFromUncompressedSingleFile(string siteFilePath)
    {
        if (File.Exists(siteFilePath))
        {
            return await File.ReadAllLinesAsync(siteFilePath);
        }

        return null;
    }

    static string[]? TryGetDataFromDatasetZipFile(string filePath)
    {
        string[] pathComponents =
            filePath.Split(
                new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries
            );

        var shallowestFolderName = pathComponents.First();

        var datasetName = shallowestFolderName;

        string zipPath = Path.Combine("Datasets", datasetName + ".zip");

        if (!File.Exists(zipPath)) return null;

        var zipEntryPath = string.Join('/', pathComponents.Skip(1));

        Debug.WriteLine("Reading from zip " + zipPath);
        return ReadLinesFromZipFileEntry(zipPath, zipEntryPath);
    }

    static string[]? TryGetDataFromSingleEntryZipFile(string filePath)
    {
        var zipPath = Path.ChangeExtension(filePath, ".zip");

        if (!File.Exists(zipPath)) return null;

        return ReadLinesFromZipFileEntry(zipPath, Path.GetFileName(filePath));
    }

    static string[]? ReadLinesFromZipFileEntry(string zipFilename, string zipEntryFilename)
    {
        using (FileStream zipFileStream = new FileStream(zipFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry? siteFileEntry = archive.GetEntry(zipEntryFilename);

                if (siteFileEntry == null)
                {
                    return null;
                }

                using (StreamReader sr = new StreamReader(siteFileEntry.Open()))
                {
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
            }
        }
    }

    private static int GetStartIndex(Regex regEx, string[] dataRows)
    {
        var index = 0;
        while (!regEx.IsMatch(dataRows[index]))
        {
            index++;
            if (index >= dataRows.Length)
            {
                throw new Exception("None of the data in the input file fits the regular expression.");
            }
        }
        return index;
    }
}
