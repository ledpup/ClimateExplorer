using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.Core.InputOutput
{
    public static class DataReader
    {
        public static async Task<List<DataSet>> ReadDataFile(string dataSetFolderName, MeasurementDefinition measurementDefinition, DataResolution dataResolution, DataAdjustment measurementType, Location? location, short? yearFilter = null)
        {
            var dataTypeFolder = GetDataTypeFolder(measurementDefinition.DataType);

            var filePath = @$"{dataTypeFolder}\{dataSetFolderName}\{dataResolution}\{measurementDefinition.FolderName}\{ (string.IsNullOrWhiteSpace(measurementDefinition.SubFolderName) ? null : measurementDefinition.SubFolderName + @"\") }";

            var regEx = new Regex(measurementDefinition.DataRowRegEx);

            var dataSets = new List<DataSet>();
            if (location != null)
            {
                foreach (var site in location.Sites)
                {
                    var dataSet = await GetDataSet(measurementDefinition, dataResolution, measurementType, yearFilter, filePath, regEx, dataSets, site);
                    if (dataSet != null)
                    {
                        dataSet.Location = location;
                        dataSets.Add(dataSet);
                    }

                }
            }
            else
            {
                var dataSet = await GetDataSet(measurementDefinition, dataResolution, measurementType, yearFilter, filePath, regEx, dataSets);
                if (dataSet != null)
                {
                    dataSets.Add(dataSet);
                }
            }
            return dataSets;
        }

        private static async Task<DataSet> GetDataSet(MeasurementDefinition measurementDefinition, DataResolution dataResolution, DataAdjustment measurementType, short? yearFilter, string filePath, Regex regEx, List<DataSet> dataSets, string site = null)
        {
            var records = await ReadDataFile(site, regEx, filePath, measurementDefinition.FileNameFormat, measurementDefinition.NullValue, dataResolution, yearFilter);

            if (records.Any())
            {
                short? startYear = null;
                if (dataResolution == DataResolution.Daily)
                {
                    startYear = records.OrderBy(x => x.Date).First(x => x.Month == 1 && x.Day == 1).Year;
                }
                else if (dataResolution == DataResolution.Monthly)
                {
                    startYear = records.OrderBy(x => x.Year).First(x => x.Month == 1).Year;
                }
                var dataSet = new DataSet
                {
                    Resolution = dataResolution,
                    DataType = measurementDefinition.DataType,
                    Station = site,
                    DataAdjustment = measurementType,
                    StartYear = startYear,
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
                case DataType.Enso:
                    return "Reference";
                case DataType.Rainfall:
                    return "Rainfall";
                default:
                    return "Temperature";
            }
        }

        static async Task<List<DataRecord>> ReadDataFile(string station, Regex regEx, string filePath, string fileName, string nullValue, DataResolution dataResolution, short? yearFilter = null)
        {
            var siteFilePath = filePath + fileName.Replace("[station]", station);

            if (!File.Exists(siteFilePath))
            {
                return new List<DataRecord>();
            }

            var rawData = await File.ReadAllLinesAsync(siteFilePath);
            var temperatureRecords = new List<DataRecord>();

            var initialDataIndex = GetStartIndex(regEx, rawData);

            var startYearRecord = short.Parse(regEx.Match(rawData[initialDataIndex]).Groups["year"].Value);

            // Check to see if the station had started recording by the year we want
            if (yearFilter != null && startYearRecord > yearFilter)
            {
                return temperatureRecords;
            }

            var startDate = yearFilter == null ? startYearRecord : yearFilter.Value;
            var date = new DateTime(startDate, 1, 1);
            var previousDate = date.AddDays(-1);
            foreach (var record in rawData)
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
                        temperatureRecords.Add(new DataRecord(date, null));
                        date = date.AddDays(1);
                    }
                    else if (dataResolution == DataResolution.Monthly)
                    {
                        temperatureRecords.Add(new DataRecord((short)date.Year, (short)date.Month, null, null));
                        date = date.AddMonths(1);
                    }
                }

                var valueString = match.Groups["value"].Value;

                float? value = string.IsNullOrWhiteSpace(valueString) || valueString == nullValue ? null : float.Parse(valueString);

                previousDate = recordDate;
                if (dataResolution == DataResolution.Daily)
                {
                    temperatureRecords.Add(new DataRecord
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
                    temperatureRecords.Add(new DataRecord
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
                var completeRecords = CompleteLastYearOfDailyData(temperatureRecords, ref date);
                completeRecords.ValidateDaily();
                return completeRecords;
            }
            else if (dataResolution == DataResolution.Monthly)
            {
                var completeRecords = CompleteLastYearOfMonthlyData(temperatureRecords, ref date);
                completeRecords.ValidateMonthly();
                return completeRecords;
            }
            throw new NotImplementedException();
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
                if (index > dataRows.Length)
                {
                    throw new Exception("None of the data in the input file fits the regular expression.");
                }
            }
            return index;
        }
    }
}
