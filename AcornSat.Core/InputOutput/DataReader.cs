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
        public static List<DataSet> ReadRawDataFile(DataSetDefinition dataSetDefinition, Location location, short? yearFilter = null)
        {
            var filePath = @$"{dataSetDefinition.DataType}\{dataSetDefinition.FolderName}\{dataSetDefinition.DataResolution}\{dataSetDefinition.RawFolderName}\";

            var siteSet = location.Sites.Where(x => File.Exists(@$"{filePath}{dataSetDefinition.RawFileName}".Replace("[station]", x))).ToList();

            var regEx = new Regex(dataSetDefinition.RawDataRowRegEx);

            var dataSets = new List<DataSet>();
            foreach (var site in siteSet)
            {
                var siteTemperatures = ReadRawDataFile(site, regEx, filePath, dataSetDefinition.RawFileName, dataSetDefinition.RawNullValue, dataSetDefinition.RawTemperatureConversion, yearFilter);
                if (siteTemperatures.Any())
                {
                    var dataSet = new DataSet
                    {
                        Location = location,
                        Resolution = DataResolution.Daily,
                        Station = site,
                        MeasurementType = MeasurementType.Unadjusted,
                        Year = yearFilter,
                        Temperatures = siteTemperatures
                    };
                    dataSets.Add(dataSet);
                }
            }
            return dataSets;
        }

        static List<TemperatureRecord> ReadRawDataFile(string station, Regex regEx, string filePath, string fileName, string nullValue, ConversionMethod conversionMethod, short? yearFilter = null)
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

                // Some recordings don't have a value for min or max. In that case the entry will be the nullValue; record a null.
                // Temp values are recorded as tenths of degrees C in ACORN-SAT raw data. Divide by 10 to get them into degrees C.
                // E.g., 222 = 22.2 degrees C
                float? max = tmax == nullValue ? null : 
                    conversionMethod == ConversionMethod.DivideBy10 ? float.Parse(tmax) / 10f : float.Parse(tmax);
                float? min = tmin == nullValue ? null : 
                    conversionMethod == ConversionMethod.DivideBy10 ? float.Parse(tmin) / 10f : float.Parse(tmin);

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

        public static List<TemperatureRecord> ReadAdjustedTemperatures(DataSetDefinition dataSetDefinition, Location location, int? yearFilter = null)
        {
            var maxMinRecords = ReadMaxMinFiles(dataSetDefinition, location);

            return ProcessMaxMinRecords(maxMinRecords, dataSetDefinition, yearFilter);
        }

        public static List<TemperatureRecord> ProcessMaxMinRecords(MaxMinRecords maxMinRecords, DataSetDefinition dataSetDefinition, int? yearFilter = null)
        {
            if (maxMinRecords.MaxRows.Length != maxMinRecords.MinRows.Length)
            {
                throw new Exception("Max and min files are not the same length");
            }

            var regEx = new Regex(dataSetDefinition.DataRowRegEx);
            var temperatureRecords = new List<TemperatureRecord>();

            var initialDataIndex = GetStartIndex(regEx, maxMinRecords.MaxRows, maxMinRecords.MinRows);

            var matchMax = regEx.Match(maxMinRecords.MaxRows[initialDataIndex]);
            var matchMin = regEx.Match(maxMinRecords.MinRows[initialDataIndex]);

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
                matchMax = regEx.Match(maxMinRecords.MaxRows[i]);
                matchMin = regEx.Match(maxMinRecords.MinRows[i]);

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
                string maxString = matchMax.Groups["temperature"].Value;
                string minString = matchMin.Groups["temperature"].Value;
                float? max = string.IsNullOrWhiteSpace(maxString) || maxString == dataSetDefinition.NullValue ? null : float.Parse(maxString);
                float? min = string.IsNullOrWhiteSpace(minString) || minString == dataSetDefinition.NullValue ? null : float.Parse(minString);

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

        private static MaxMinRecords ReadMaxMinFiles(DataSetDefinition dataSetDefinition, Location location)
        {
            var maximumsFilePath = string.Empty;
            var minimumsFilePath = string.Empty;
            foreach (var site in location.Sites)
            {
                var maximumsFileName = @$"{dataSetDefinition.MaxTempFileName}".Replace("[station]", site);
                maximumsFilePath = @$"{dataSetDefinition.DataType}\{dataSetDefinition.FolderName}\{dataSetDefinition.DataResolution}\{dataSetDefinition.MaxTempFolderName}\" + maximumsFileName;
                var minimumsFileName = @$"{dataSetDefinition.MinTempFileName}".Replace("[station]", site);
                minimumsFilePath = @$"{dataSetDefinition.DataType}\{dataSetDefinition.FolderName}\{dataSetDefinition.DataResolution}\{dataSetDefinition.MinTempFolderName}\" + minimumsFileName;

                if (File.Exists(maximumsFilePath) && File.Exists(minimumsFilePath))
                {
                    break;
                }
            }

            var maximums = File.ReadAllLines(maximumsFilePath);
            var minimums = File.ReadAllLines(minimumsFilePath);

            var maxMinRecords = new MaxMinRecords
            {
                MaxRows = maximums,
                MinRows = minimums,
            };

            return maxMinRecords;
        }

        private static int GetStartIndex(Regex regEx, string[] dataRows, string[]? correspondingDataRows = null)
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
            if (correspondingDataRows != null && !regEx.IsMatch(correspondingDataRows[index]))
            {
                throw new Exception($"The two sets of data do not start to match at the same index ({index}).");
            }
            return index;
        }
    }
}
