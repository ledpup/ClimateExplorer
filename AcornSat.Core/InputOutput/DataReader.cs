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
        public static List<DataSet> ReadMinMaxTemperatureDataFile(string dataType, string dataSetFolderName, MeasurementDefinition measurementDefinition, DataResolution dataResolution, MeasurementType measurementType, Location location, short? yearFilter = null)
        {
            var filePath = @$"{dataType}\{dataSetFolderName}\{dataResolution}\{measurementDefinition.FolderName}\{ (string.IsNullOrWhiteSpace(measurementDefinition.SubFolderName) ? null : measurementDefinition.SubFolderName + @"\") }";

            var siteSet = location.Sites.Where(x => File.Exists(@$"{filePath}{measurementDefinition.FileNameFormat}".Replace("[station]", x))).ToList();

            var regEx = new Regex(measurementDefinition.DataRowRegEx);

            var dataSets = new List<DataSet>();
            foreach (var site in siteSet)
            {
                var siteTemperatures = ReadMinMaxTemperatureDataFile(site, regEx, filePath, measurementDefinition.FileNameFormat, measurementDefinition.NullValue, yearFilter);
                if (siteTemperatures.Any())
                {
                    var dataSet = new DataSet
                    {
                        Location = location,
                        Resolution = dataResolution,
                        Station = site,
                        MeasurementType = measurementType,
                        Year = yearFilter,
                        Temperatures = siteTemperatures
                    };
                    dataSets.Add(dataSet);
                }
            }
            return dataSets;
        }

        static List<TemperatureRecord> ReadMinMaxTemperatureDataFile(string station, Regex regEx, string filePath, string fileName, string nullValue, short? yearFilter = null)
        {
            var siteFilePath = filePath + fileName.Replace("[station]", station);
            var rawData = File.ReadAllLines(siteFilePath);
            var temperatureRecords = new List<TemperatureRecord>();

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
                var day = short.Parse(match.Groups["day"].Value);

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
                    temperatureRecords.Add(new TemperatureRecord(date, null, null));
                    date = date.AddDays(1);
                }

                var tmax = match.Groups["tmax"].Value;
                var tmin = match.Groups["tmin"].Value;

                float? max = tmax == nullValue ? null : float.Parse(tmax);
                float? min = tmin == nullValue ? null : float.Parse(tmin);

                temperatureRecords.Add(new TemperatureRecord
                {
                    Day = day,
                    Month = month,
                    Year = year,
                    Min = min,
                    Max = max,
                });
                previousDate = recordDate;
                date = date.AddDays(1);
            }

            var completeRecords = CompleteLastYearOfDailyData(temperatureRecords, ref date);

            completeRecords.ValidateDaily();

            return completeRecords;
        }

        private static List<TemperatureRecord> CompleteLastYearOfDailyData(List<TemperatureRecord> temperatureRecords, ref DateTime date)
        {
            if (!temperatureRecords.Any() || date.DayOfYear == 1)
            {
                return temperatureRecords;
            }

            var endYear = date.Year;
            do
            {
                temperatureRecords.Add(new TemperatureRecord(date, null, null));
                date = date.AddDays(1);
            } while (endYear == date.Year);

            return temperatureRecords;
        }

        private static List<TemperatureRecord> CompleteLastYearOfMonthlyData(List<TemperatureRecord> temperatureRecords, ref DateTime date)
        {
            if (!temperatureRecords.Any() || date.Month == 12)
            {
                return temperatureRecords;
            }

            var endYear = date.Year;
            do
            {
                temperatureRecords.Add(new TemperatureRecord((short)date.Year, (short)date.Month, null, null, null));
                date = date.AddMonths(1);
            } while (endYear == date.Year);

            return temperatureRecords;
        }

        public class MaxMinRecords
        {
            public string[] MaxRows { get; set; }
            public string[] MinRows { get; set; }
        }

        public static List<DataSet> ReadPairedTemperatureFiles(DataSetDefinition dataSetDefinition, MeasurementType measurementType, MeasurementDefinition maxTempDefinition, MeasurementDefinition minTempDefinition, Location location, short? yearFilter = null)
        {
            var dataSets = new List<DataSet>();
            foreach (var site in location.Sites)
            {
                var maxMinRecords = ReadMaxMinFiles("Temperature", dataSetDefinition.FolderName, dataSetDefinition.DataResolution, maxTempDefinition, minTempDefinition, site);
                var temperatures = ProcessMaxMinRecords(maxMinRecords, dataSetDefinition, maxTempDefinition, minTempDefinition, yearFilter);

                if (temperatures.Any())
                {
                    var dataSet = new DataSet
                    {
                        Location = location,
                        Resolution = dataSetDefinition.DataResolution,
                        Station = site,
                        MeasurementType = measurementType,
                        Year = yearFilter,
                        Temperatures = temperatures
                    };
                    dataSets.Add(dataSet);
                }
            }
            return dataSets;
        }

        public static List<TemperatureRecord> ProcessMaxMinRecords(MaxMinRecords maxMinRecords, DataSetDefinition dataSetDefinition, MeasurementDefinition maxTempDefinition, MeasurementDefinition minTempDefinition, int? yearFilter = null)
        {
            var temperatureRecords = new List<TemperatureRecord>();
            if (maxMinRecords == null)
            {
                return temperatureRecords;
            }
            if (maxMinRecords.MaxRows.Length != maxMinRecords.MinRows.Length)
            {
                Console.WriteLine("Max and min files are not the same length");
            }

            var maxRegEx = new Regex(maxTempDefinition.DataRowRegEx);
            var minRegEx = new Regex(minTempDefinition.DataRowRegEx);
            

            var initialDataIndex = GetStartIndex(maxRegEx, minRegEx, maxMinRecords.MaxRows, maxMinRecords.MinRows);

            var matchMax = maxRegEx.Match(maxMinRecords.MaxRows[initialDataIndex]);
            var matchMin = minRegEx.Match(maxMinRecords.MinRows[initialDataIndex]);

            var startYearMax = int.Parse(matchMax.Groups["year"].Value);
            var startYearMin = int.Parse(matchMin.Groups["year"].Value);

            if (startYearMax != startYearMin)
            {
                throw new Exception($"The min and max files do not start on the same year ({startYearMax} vs {startYearMin})");
            }

            // If we know we can't have data for the year requested, exit early
            if (yearFilter != null && startYearMax > yearFilter)
            {
                return temperatureRecords;
            }

            var startDate = yearFilter == null ? startYearMax : yearFilter.Value;
            var date = new DateTime(startDate, 1, 1);

            for (var i = initialDataIndex; i < maxMinRecords.MaxRows.Length; i++)
            {
                matchMax = maxRegEx.Match(maxMinRecords.MaxRows[i]);
                matchMin = minRegEx.Match(maxMinRecords.MinRows[i]);

                if (!matchMax.Success)
                {
                    throw new Exception($"The data row {maxMinRecords.MaxRows[i]} does not match the regulator expression for this data set");
                }
                if (!matchMin.Success)
                {
                    throw new Exception($"The data row {maxMinRecords.MinRows[i]} does not match the regulator expression for this data set");
                }

                var maxYear = int.Parse(matchMax.Groups["year"].Value);
                var minYear = int.Parse(matchMin.Groups["year"].Value);

                var maxMonth = int.Parse(matchMax.Groups["month"].Value);
                var minMonth = int.Parse(matchMin.Groups["month"].Value);

                var maxDay = 1;
                var minDay = 1;
                if (dataSetDefinition.DataResolution == DataResolution.Daily)
                {
                    maxDay = int.Parse(matchMax.Groups["day"].Value);
                    minDay = int.Parse(matchMin.Groups["day"].Value);
                }

                if (maxYear != minYear || maxMonth != minMonth || maxDay != minDay)
                {
                    throw new Exception("Max and min dates do not match");
                }

                if (yearFilter != null)
                {
                    if (maxYear < yearFilter)
                    {
                        continue;
                    }
                    else if (maxYear > yearFilter)
                    {
                        break;
                    }
                }

                var recordDate = new DateTime(maxYear, maxMonth, maxDay);

                while (recordDate > date)
                {
                    if (dataSetDefinition.DataResolution == DataResolution.Daily)
                    {
                        temperatureRecords.Add(new TemperatureRecord(date, null, null));
                        date = date.AddDays(1);
                    }
                    else if (dataSetDefinition.DataResolution == DataResolution.Monthly)
                    {
                        temperatureRecords.Add(new TemperatureRecord((short)date.Year, (short)date.Month, null, null, null));
                        date = date.AddMonths(1);
                    }
                }
                string maxString = matchMax.Groups["value"].Value;
                string minString = matchMin.Groups["value"].Value;
                float? max = string.IsNullOrWhiteSpace(maxString) || maxString == maxTempDefinition.NullValue ? null : float.Parse(maxString);
                float? min = string.IsNullOrWhiteSpace(minString) || minString == minTempDefinition.NullValue ? null : float.Parse(minString);

                if (dataSetDefinition.DataResolution == DataResolution.Daily)
                {
                    temperatureRecords.Add(new TemperatureRecord(date, min, max));
                    date = date.AddDays(1);
                }
                else if (dataSetDefinition.DataResolution == DataResolution.Monthly)
                {
                    temperatureRecords.Add(new TemperatureRecord((short)date.Year, (short)date.Month, null, min, max));
                    date = date.AddMonths(1);
                }
            }

            
            if (dataSetDefinition.DataResolution == DataResolution.Daily)
            {
                var completeRecords = CompleteLastYearOfDailyData(temperatureRecords, ref date);
                completeRecords.ValidateDaily();
                return completeRecords;
            }
            else if (dataSetDefinition.DataResolution == DataResolution.Monthly)
            {
                var completeRecords = CompleteLastYearOfMonthlyData(temperatureRecords, ref date);
                completeRecords.ValidateMonthly();
                return completeRecords;
            }
            throw new NotImplementedException();
        }

        private static MaxMinRecords ReadMaxMinFiles(string dataType, string datasetFolderName, DataResolution dataResolution, MeasurementDefinition maxTempDefintion, MeasurementDefinition minTempDefintion, string station)
        {           
            var maximumsFileName = @$"{maxTempDefintion.FileNameFormat}".Replace("[station]", station);
            var maximumsFilePath = @$"{dataType}\{datasetFolderName}\{dataResolution}\{maxTempDefintion.FolderName}\{ ( string.IsNullOrWhiteSpace(maxTempDefintion.SubFolderName) ? null : maxTempDefintion.SubFolderName + @"\") }" + maximumsFileName;

            var minimumsFileName = @$"{minTempDefintion.FileNameFormat}".Replace("[station]", station);
            var minimumsFilePath = @$"{dataType}\{datasetFolderName}\{dataResolution}\{maxTempDefintion.FolderName}\{ (string.IsNullOrWhiteSpace(minTempDefintion.SubFolderName) ? null : minTempDefintion.SubFolderName + @"\") }" + minimumsFileName;

            if (File.Exists(maximumsFilePath) && File.Exists(minimumsFilePath))
            {
                var maximums = File.ReadAllLines(maximumsFilePath);
                var minimums = File.ReadAllLines(minimumsFilePath);

                var maxMinRecords = new MaxMinRecords
                {
                    MaxRows = maximums,
                    MinRows = minimums,
                };

                return maxMinRecords;
            }
            
            Console.WriteLine($"Cannot find data files for station {station}.");
            return null;
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

        private static int GetStartIndex(Regex maxRegEx, Regex minRegEx, string[] dataRows, string[] correspondingDataRows)
        {
            var index = 0;
            while (!maxRegEx.IsMatch(dataRows[index]))
            {
                index++;
                if (index > dataRows.Length)
                {
                    throw new Exception("None of the data in the input file fits the regular expression.");
                }
            }
            if (correspondingDataRows != null && !minRegEx.IsMatch(correspondingDataRows[index]))
            {
                throw new Exception($"The two sets of data do not start to match at the same index ({index}).");
            }
            return index;
        }
    }
}
