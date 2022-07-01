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
    public static async Task<DataSet> GetDataSet(string dataSetFolderName, MeasurementDefinition measurementDefinition, DataResolution dataResolution, Location? location, short? yearFilter = null)
    {
        var dataTypeFolder = GetDataTypeFolder(measurementDefinition.DataType);

        var filePath = @$"{dataTypeFolder}\{dataSetFolderName}\{dataResolution}";

        if (!string.IsNullOrWhiteSpace(measurementDefinition.FolderName))
            filePath += "\\" + measurementDefinition.FolderName;

        if (!string.IsNullOrWhiteSpace(measurementDefinition.SubFolderName))
            filePath += "\\" + measurementDefinition.SubFolderName;

        filePath += "\\";

        var regEx = new Regex(measurementDefinition.DataRowRegEx);

        var dataSets = new List<DataSet>();

        var dataSet = new DataSet
        {
            Resolution = dataResolution,
            MeasurementDefinition = measurementDefinition.ToViewModel(),
            Year = yearFilter,
            Location = location
        };

        var records = new Dictionary<string, DataRecord>();
        if (location != null)
        {
            var processedStations = new List<string>();

            foreach (var station in location.Stations)
            {
                if (!measurementDefinition.UseStationDatesWhenCompilingAcrossFiles && processedStations.Contains(station.Id))
                {
                    continue;
                }

                var stationRecords = await ReadDataFile(station.Id, regEx, filePath, measurementDefinition.FileNameFormat, measurementDefinition.NullValue, dataResolution, yearFilter);

                if (measurementDefinition.UseStationDatesWhenCompilingAcrossFiles)
                {
                    stationRecords = stationRecords
                                                .Where(x =>    (station.StartDate == null || x.Value.Date >= station.StartDate)
                                                            && (station.EndDate == null   || x.Value.Date <= station.EndDate))
                                                .ToDictionary(x => x.Key, y => y.Value);
                }

                // There may still be duplicates. Only add records if it won't create a duplicate
                foreach (var stationRecord in stationRecords)
                {
                    if (!records.ContainsKey(stationRecord.Key))
                    {
                        records.Add(stationRecord.Key, stationRecord.Value);
                    }
                }
            }
        }
        else
        {
            records = await ReadDataFile(String.Empty, regEx, filePath, measurementDefinition.FileNameFormat, measurementDefinition.NullValue, dataResolution, yearFilter);
        }

        if (!records.Any())
        {
            return null;
        }
        dataSet.DataRecords = records.Values.ToList();
        return dataSet;
    }

    private static object GetDataTypeFolder(DataType dataType)
    {
        switch (dataType)
        {
            case DataType.MEIv2:
            case DataType.CO2:
            case DataType.CH4:
            case DataType.N2O:
            case DataType.IOD:
                return "Reference";
            case DataType.Rainfall:
                return "Rainfall";
            case DataType.TempMax:
            case DataType.TempMin:
                return "Temperature";
            case DataType.SolarRadiation:
                return "SolarRadiation";
        }

        throw new NotImplementedException();
    }

    static async Task<Dictionary<string, DataRecord>> ReadDataFile(
        string station,
        Regex regEx,
        string filePath,
        string fileName,
        string nullValue,
        DataResolution dataResolution,
        short? yearFilter = null)
    {
        var siteFilePath = filePath + fileName.Replace("[station]", station);

        string[]? lines = await GetLinesInDataFileWithCascade(siteFilePath);

        if (lines == null)
        {
            // We couldn't find the data in any of the expected locations
            return new Dictionary<string, DataRecord>();
        }

        var dataRecords = new Dictionary<string, DataRecord>();

        var initialDataIndex = GetStartIndex(regEx, lines);

        var startYearRecord = short.Parse(regEx.Match(lines[initialDataIndex]).Groups["year"].Value);

        // Check to see if the station had started recording by the year we want
        if (yearFilter != null && startYearRecord > yearFilter)
        {
            return dataRecords;
        }

        var startDate = yearFilter == null ? startYearRecord : yearFilter.Value;
        var date = new DateTime(startDate, 1, 1);
        var previousDate = date.AddDays(-1);
        foreach (var line in lines)
        {
            var match = regEx.Match(line);

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

            if (yearFilter != null)
            {
                if (year < yearFilter)
                {
                    continue;
                }
                else if (year > yearFilter)
                {
                    break;
                }
            }

            var recordDate = new DateTime(year, month, day);
            if (recordDate <= previousDate)
            {
                Console.Error.WriteLine($"Date of current record ({recordDate}) is earlier than or equal to the previous date ({previousDate}). The file is not ordered by date properly and/or there are duplicate records. Will skip this record.");
                continue;
            }
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

        if (dataResolution == DataResolution.Daily)
        {
            var completeRecords = CompleteLastYearOfDailyData(dataRecords, ref date);
            completeRecords.Values.ValidateDaily();
            return completeRecords;
        }
        else if (dataResolution == DataResolution.Monthly)
        {
            var completeRecords = CompleteLastYearOfMonthlyData(dataRecords, ref date);
            completeRecords.Values.ValidateMonthly();
            return completeRecords;
        }
        throw new NotImplementedException();
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

    private static Dictionary<string, DataRecord> CompleteLastYearOfDailyData(Dictionary<string, DataRecord> records, ref DateTime date)
    {
        if (!records.Any() || date.DayOfYear == 1)
        {
            return records;
        }

        var endYear = date.Year;
        do
        {
            var record = new DataRecord(date, null);
            records.Add(record.Key, record);
            date = date.AddDays(1);
        } while (endYear == date.Year);

        return records;
    }

    private static Dictionary<string, DataRecord> CompleteLastYearOfMonthlyData(Dictionary<string, DataRecord> temperatureRecords, ref DateTime date)
    {
        if (!temperatureRecords.Any() || date.Month == 12)
        {
            return temperatureRecords;
        }

        var endYear = date.Year;
        do
        {
            var record = new DataRecord((short)date.Year, (short)date.Month, null, null);
            temperatureRecords.Add(record.Key, record);
            date = date.AddMonths(1);
        } while (endYear == date.Year);

        return temperatureRecords;
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
