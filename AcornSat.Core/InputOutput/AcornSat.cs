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
    public static class AcornSat
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

            var completeDataSet = CompleteLastYearOfData(temperatureRecords, ref date);
            
            var calendar = new GregorianCalendar();
            var invalidData = completeDataSet.GroupBy(x => x.Year).Where(x => x.Count() > calendar.GetDaysInYear(x.Key));
            if (invalidData.Any())
            {
                throw new Exception($"Data is invalid. More than a year worht of records for the years { string.Join(", ", invalidData.Select(x => x.Key)) }");
            }
            var duplicateDates = completeDataSet.GroupBy(x => x.Date)
                                              .Where(x => x.Count() > 1)
                                              .Select(x => x.Key)
                                              .ToList();
            if (duplicateDates.Any())
            {
                throw new Exception($"There are duplicate dates ({string.Join(", ", duplicateDates.Select(x => x.ToShortDateString()))}. The file is corrupt.");
            }

            return completeDataSet;
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
            var maximumsFilePath = string.Empty;
            var minimumsFilePath = string.Empty;
            var siteWithData = string.Empty;
            foreach (var site in location.Sites)
            {
                maximumsFilePath = @$"{dataSetDefinition.DataType}\{dataSetDefinition.FolderName}\{dataSetDefinition.DataResolution}\{dataSetDefinition.MaxTempFolderName}\{dataSetDefinition.MaxTempFileName}".Replace("[station]", site);
                minimumsFilePath = @$"{dataSetDefinition.DataType}\{dataSetDefinition.FolderName}\{dataSetDefinition.DataResolution}\{dataSetDefinition.MinTempFolderName}\{dataSetDefinition.MinTempFileName}".Replace("[station]", site);

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

            var startYearMax = int.Parse(maximums[2].Substring(0, 4));
            var startYearMin = int.Parse(minimums[2].Substring(0, 4));

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

            for (var i = 2; i < maximums.Length; i++)
            {
                var splitMin = minimums[i].Split(',');
                var splitMax = maximums[i].Split(',');

                var maxDate = DateTime.Parse(splitMax[0]);
                var minDate = DateTime.Parse(splitMin[0]);
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

                float? min = string.IsNullOrWhiteSpace(splitMin[1]) ? null : float.Parse(splitMin[1]);
                float? max = string.IsNullOrWhiteSpace(splitMax[1]) ? null : float.Parse(splitMax[1]);

                temperatureRecords.Add(new TemperatureRecord(date, min, max));
                date = date.AddDays(1);
            }

            var completeDataSet = CompleteLastYearOfData(temperatureRecords, ref date);

            var calendar = new GregorianCalendar();
            var invalidData = completeDataSet.GroupBy(x => x.Year).Where(x => x.Count() > calendar.GetDaysInYear(x.Key));
            if (invalidData.Any())
            {
                throw new Exception($"Data is invalid. More than a year worht of records for the years { string.Join(", ", invalidData.Select(x => x.Key)) }");
            }
            var duplicateDates = completeDataSet.GroupBy(x => x.Date)
                                  .Where(x => x.Count() > 1)
                                  .Select(x => x.Key)
                                  .ToList();
            if (duplicateDates.Any())
            {
                throw new Exception($"There are duplicate dates ({string.Join(", ", duplicateDates.Select(x => x.ToShortDateString()))}. The file is corrupt.");
            }

            return completeDataSet;
        }
    }
}
