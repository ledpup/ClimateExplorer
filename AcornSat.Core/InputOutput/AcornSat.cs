using System;
using System.Collections.Generic;
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
            var siteSet = location.Sites.Where(x => File.Exists(@$"Temperature\Daily\raw-data\hqnew{x}.txt")).ToList();

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

        public static List<TemperatureRecord> ReadRawDataFile(string station, short? yearFilter = null)
        {
            var rawTempsRegEx = new Regex(@"\s+(-*\d+)\s+(-*\d+)");
            var siteFilePath = @$"Temperature\Daily\raw-data\hqnew{station}.txt";
            var rawData = File.ReadAllLines(siteFilePath);
            var temperatureRecords = new List<TemperatureRecord>();

            var startYearRecord = short.Parse(rawData[0].Substring(6, 4));
            var startYear = yearFilter == null ? startYearRecord : yearFilter.Value;
            var date = new DateTime(startYear, 1, 1);

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
                date = date.AddDays(1);
            }

            return CompleteLastYearOfData(temperatureRecords, ref date);
        }

        private static List<TemperatureRecord> CompleteLastYearOfData(List<TemperatureRecord> temperatureRecords, ref DateTime date)
        {
            if (date.DayOfYear == 1)
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

        public static List<TemperatureRecord> ReadAdjustedTemperatures(Location location, int? yearFilter = null)
        {
            var maximumsFilePath = string.Empty;
            var minimumsFilePath = string.Empty;
            var siteWithData = string.Empty;
            foreach (var site in location.Sites)
            {
                maximumsFilePath = @$"Temperature\Daily\daily_tmax\tmax.{site}.daily.csv";
                minimumsFilePath = @$"Temperature\Daily\daily_tmin\tmin.{site}.daily.csv";

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

            var startYear = yearFilter == null ? startYearMin : yearFilter.Value;
            var date = new DateTime(startYear, 1, 1);

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
                    if (date.Year < yearFilter)
                    {
                        continue;
                    }
                    else if (date.Year > yearFilter)
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

            return CompleteLastYearOfData(temperatureRecords, ref date);
        }
    }
}
