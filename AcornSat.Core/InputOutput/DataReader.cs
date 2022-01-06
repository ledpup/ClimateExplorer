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
        public static List<DataSet> ReadRawDataFile(Location location, short? yearFilter = null)
        {
            var siteSet = location.Sites.Where(x => File.Exists(@$"Temperature\ACORN-SAT\Daily\raw-data\hqnew{x}.txt")).ToList();

            var dataSets = new List<DataSet>();
            foreach (var site in siteSet)
            {
                var siteTemperatures = ReadRawDataFile(site, yearFilter);
                if (siteTemperatures.Any())
                {
                    var dataSet = new DataSet
                    {
                        Location = location,
                        Resolution = DataResolution.Daily,
                        Station = site,
                        Type = MeasurementType.Unadjusted,
                        Year = yearFilter,
                        Temperatures = siteTemperatures
                    };
                    dataSets.Add(dataSet);
                }
            }
            return dataSets;
        }

        static List<TemperatureRecord> ReadRawDataFile(string station, short? yearFilter = null)
        {
            var rawTempsRegEx = new Regex(@"\s+(-*\d+)\s+(-*\d+)");
            var siteFilePath = @$"Temperature\ACORN-SAT\Daily\raw-data\hqnew{station}.txt";
            var rawData = File.ReadAllLines(siteFilePath);
            var temperatureRecords = new List<TemperatureRecord>();

            var startYearRecord = short.Parse(rawData[0].Substring(6, 4));

            // Check to see if the station had started recording by the year we want
            if (yearFilter != null && startYearRecord > yearFilter)
            {
                return temperatureRecords;
            }

            var startDate = yearFilter == null ? startYearRecord : yearFilter.Value;
            var date = new DateTime(startDate, 1, 1);
            var previousDate = date;
            foreach (var record in rawData)
            {
                var year = short.Parse(record.Substring(6, 4));
                var month = short.Parse(record.Substring(10, 2));
                var day = short.Parse(record.Substring(12, 2));

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

                var temps = record.Substring(13);
                var tempGroups = rawTempsRegEx.Match(temps).Groups;

                // Some recordings don't have a value for min or max. In that case the entry will be -999. Will make those values null
                // Temp values are recorded as tenths of degrees C in ACORN-SAT raw data. Divide by 10 to get them into degrees C.
                // E.g., 222 = 22.2 degrees C
                float? max = tempGroups[1].Value == "-999" ? null : float.Parse(tempGroups[1].Value) / 10;
                float? min = tempGroups[2].Value == "-999" ? null : float.Parse(tempGroups[2].Value) / 10;

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

            var completeRecords = CompleteLastYearOfData(temperatureRecords, ref date);

            completeRecords.ValidateDaily();

            return completeRecords;
        }

        private static List<TemperatureRecord> CompleteLastYearOfData(List<TemperatureRecord> temperatureRecords, ref DateTime date)
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

        public static List<TemperatureRecord> ReadAdjustedTemperatures(DataSetDefinition dataSetDefinition, Location location, int? yearFilter = null)
        {
            var regEx = new Regex(dataSetDefinition.DataRowRegEx);
            var maximumsFileName = string.Empty;
            var minimumsFileName = string.Empty;
            var maximumsFilePath = string.Empty;
            var minimumsFilePath = string.Empty;
            var siteWithData = string.Empty;
            foreach (var site in location.Sites)
            {
                maximumsFileName = @$"{dataSetDefinition.MaxTempFileName}".Replace("[station]", site);
                maximumsFilePath = @$"{dataSetDefinition.DataType}\{dataSetDefinition.FolderName}\{dataSetDefinition.DataResolution}\{dataSetDefinition.MaxTempFolderName}\" + maximumsFileName;
                minimumsFileName = @$"{dataSetDefinition.MinTempFileName}".Replace("[station]", site);
                minimumsFilePath = @$"{dataSetDefinition.DataType}\{dataSetDefinition.FolderName}\{dataSetDefinition.DataResolution}\{dataSetDefinition.MinTempFolderName}\" + minimumsFileName;
                
                if (File.Exists(maximumsFilePath) && File.Exists(minimumsFilePath))
                {
                    siteWithData = site;
                    break;
                }
            }

            var maximums = File.ReadAllLines(maximumsFilePath);
            var minimums = File.ReadAllLines(minimumsFilePath);

            if (maximums.Length != minimums.Length)
            {
                throw new Exception("Max and min files are not the same length");
            }

            var temperatureRecords = new List<TemperatureRecord>();

            var initialDataIndex = GetStartIndex(maximums, minimums, regEx);

            var startYearMax = int.Parse(maximums[initialDataIndex].Substring(0, 4));
            var startYearMin = int.Parse(minimums[initialDataIndex].Substring(0, 4));

            if (startYearMax != startYearMin)
            {
                throw new Exception($"The min and max files for {siteWithData} do not start on the same year ({startYearMax} vs {startYearMin})");
            }

            // If we know we can't have data for the year requested, exit early
            if (yearFilter != null && startYearMax > yearFilter)
            {
                return temperatureRecords;
            }

            var startDate = yearFilter == null ? startYearMax : yearFilter.Value;
            var date = new DateTime(startDate, 1, 1);

            for (var i = initialDataIndex; i < maximums.Length; i++)
            {

                var matchMax = regEx.Match(maximums[i]);
                var matchMin = regEx.Match(minimums[i]);

                if (!matchMax.Success)
                {
                    throw new Exception($"The data row {maximums[i]} does not match the regulator expression in the file {maximumsFileName}");
                }
                if (!matchMin.Success)
                {
                    throw new Exception($"The data row {minimums[i]} does not match the regulator expression in the file {minimumsFileName}");
                }

                var maxDate = DateTime.Parse($"{matchMax.Groups["year"].Value}-{matchMax.Groups["month"].Value}-{matchMax.Groups["day"].Value}");
                var minDate = DateTime.Parse($"{matchMin.Groups["year"].Value}-{matchMin.Groups["month"].Value}-{matchMin.Groups["day"].Value}");
                if (maxDate != minDate)
                {
                    throw new Exception("Max and min dates do not match");
                }

                if (yearFilter != null)
                {
                    if (maxDate.Year < yearFilter)
                    {
                        continue;
                    }
                    else if (maxDate.Year > yearFilter)
                    {
                        break;
                    }
                }

                while (maxDate > date)
                {
                    temperatureRecords.Add(new TemperatureRecord(date, null, null));
                    date = date.AddDays(1);
                }
                string maxString = matchMax.Groups["temperature"].Value;
                string minString = matchMin.Groups["temperature"].Value;
                float? max = string.IsNullOrWhiteSpace(maxString) ? null : float.Parse(maxString);
                float? min = string.IsNullOrWhiteSpace(minString) ? null : float.Parse(minString);

                temperatureRecords.Add(new TemperatureRecord(date, min, max));
                date = date.AddDays(1);
            }

            var completeRecords = CompleteLastYearOfData(temperatureRecords, ref date);

            completeRecords.ValidateDaily();

            return completeRecords;
        }

        private static int GetStartIndex(string[] maximums, string[] minimums, Regex regEx)
        {
            var index = 0;
            while (!regEx.IsMatch(maximums[index]))
            {
                index++;
                if (index > maximums.Length)
                {
                    throw new Exception("None of the data in the input file fits the regular expression.");
                }
            }
            return index;
        }
    }
}
