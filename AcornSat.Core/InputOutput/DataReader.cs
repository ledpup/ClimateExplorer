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

namespace AcornSat.Core.InputOutput
{
    public static class DataReader
    {
        public static async Task<List<DataSet>> ReadDataFile(string dataSetFolderName, MeasurementDefinition measurementDefinition, DataResolution dataResolution, Location? location, short? yearFilter = null)
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
            if (location != null)
            {
                foreach (var site in location.Sites)
                {
                    var dataSet = await GetDataSet(measurementDefinition, dataResolution, yearFilter, filePath, regEx, dataSets, site);
                    if (dataSet != null)
                    {
                        dataSet.Location = location;
                        dataSets.Add(dataSet);
                    }

                }
            }
            else
            {
                var dataSet = await GetDataSet(measurementDefinition, dataResolution, yearFilter, filePath, regEx, dataSets);
                if (dataSet != null)
                {
                    dataSets.Add(dataSet);
                }
            }
            return dataSets;
        }

        private static async Task<DataSet> GetDataSet(MeasurementDefinition measurementDefinition, DataResolution dataResolution, short? yearFilter, string filePath, Regex regEx, List<DataSet> dataSets, string site = null)
        {
            var records = await ReadDataFile(site, regEx, filePath, measurementDefinition.FileNameFormat, measurementDefinition.NullValue, dataResolution, yearFilter);

            if (records.Any())
            {
                var dataSet = new DataSet
                {
                    Resolution = dataResolution,
                    MeasurementDefinition = measurementDefinition.ToViewModel(),
                    Station = site,
                    Year = yearFilter,
                    DataRecords = records
                };
                return dataSet;
            }
            return null;
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

        static async Task<List<DataRecord>> ReadDataFile(
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
                return new List<DataRecord>();
            }

            var dataRecords = new List<DataRecord>();

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
            foreach (var record in lines)
            {
                var match = regEx.Match(record);

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
                        dataRecords.Add(new DataRecord(date, null));
                        date = date.AddDays(1);
                    }
                    else if (dataResolution == DataResolution.Monthly)
                    {
                        dataRecords.Add(new DataRecord((short)date.Year, (short)date.Month, null, null));
                        date = date.AddMonths(1);
                    }
                }

                var valueString = match.Groups["value"].Value;

                float? value = string.IsNullOrWhiteSpace(valueString) || valueString == nullValue ? null : float.Parse(valueString);

                previousDate = recordDate;
                if (dataResolution == DataResolution.Daily)
                {
                    dataRecords.Add(new DataRecord
                    {
                        Day = day,
                        Month = month,
                        Year = year,
                        Value = value,
                    });
                    date = date.AddDays(1);
                }
                else if (dataResolution == DataResolution.Monthly)
                {
                    dataRecords.Add(new DataRecord
                    {
                        Month = month,
                        Year = year,
                        Value = value,
                    });
                    date = date.AddMonths(1);
                }
            }

            if (dataResolution == DataResolution.Daily)
            {
                var completeRecords = CompleteLastYearOfDailyData(dataRecords, ref date);
                completeRecords.ValidateDaily();
                return completeRecords;
            }
            else if (dataResolution == DataResolution.Monthly)
            {
                var completeRecords = CompleteLastYearOfMonthlyData(dataRecords, ref date);
                completeRecords.ValidateMonthly();
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

        private static List<DataRecord> CompleteLastYearOfDailyData(List<DataRecord> temperatureRecords, ref DateTime date)
        {
            if (!temperatureRecords.Any() || date.DayOfYear == 1)
            {
                return temperatureRecords;
            }

            var endYear = date.Year;
            do
            {
                temperatureRecords.Add(new DataRecord(date, null));
                date = date.AddDays(1);
            } while (endYear == date.Year);

            return temperatureRecords;
        }

        private static List<DataRecord> CompleteLastYearOfMonthlyData(List<DataRecord> temperatureRecords, ref DateTime date)
        {
            if (!temperatureRecords.Any() || date.Month == 12)
            {
                return temperatureRecords;
            }

            var endYear = date.Year;
            do
            {
                temperatureRecords.Add(new DataRecord((short)date.Year, (short)date.Month, null, null));
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
}
