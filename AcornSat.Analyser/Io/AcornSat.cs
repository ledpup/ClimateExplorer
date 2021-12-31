using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.Analyser.Io
{
    public static class AcornSatIo
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
            var rawDataRecords = new List<TemperatureRecord>();
            foreach (var record in rawData)
            {
                var year = short.Parse(record.Substring(6, 4));
                var month = short.Parse(record.Substring(10, 2));
                var day = short.Parse(record.Substring(12, 2));
                var temps = record.Substring(13);
                var tempGroups = rawTempsRegEx.Match(temps).Groups;

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

                // Some recordings don't have a value for min or max. In that case the entry will be -999. Will make those values null
                // Temp values are recorded as tenths of degrees C in ACORN-SAT raw data. Divide by 10 to get them into degrees C.
                // E.g., 222 = 22.2 degrees C
                float? max = tempGroups[1].Value == "-999" ? null : float.Parse(tempGroups[1].Value) / 10;
                float? min = tempGroups[2].Value == "-999" ? null : float.Parse(tempGroups[2].Value) / 10;

                rawDataRecords.Add(new TemperatureRecord
                {
                    Day = day,
                    Month = month,
                    Year = year,
                    Min = min,
                    Max = max,
                });
            }
            return rawDataRecords;
        }

        public static List<TemperatureRecord> ReadAdjustedTemperatures(Location location, int? year = null)
        {
            var maximumsFilePath = string.Empty;
            var minimumsFilePath = string.Empty;
            foreach (var site in location.Sites)
            {
                maximumsFilePath = @$"Temperature\Daily\daily_tmax\tmax.{site}.daily.csv";
                minimumsFilePath = @$"Temperature\Daily\daily_tmin\tmin.{site}.daily.csv";

                if (File.Exists(maximumsFilePath) && File.Exists(minimumsFilePath))
                {
                    break;
                }
            }

            var maximums = File.ReadAllLines(maximumsFilePath);
            var minimums = File.ReadAllLines(minimumsFilePath);

            if (maximums.Length != minimums.Length)
            {
                throw new Exception("Max and min files are not the same length");
            }

            var adjustedTemperatureRecords = new List<TemperatureRecord>();
            for (var i = 2; i < maximums.Length; i++)
            {
                var splitMin = minimums[i].Split(',');
                var splitMax = maximums[i].Split(',');

                var date = DateTime.Parse(splitMax[0]);
                var minDate = DateTime.Parse(splitMin[0]);

                if (date != minDate)
                {
                    throw new Exception("Max and min dates do not match");
                }

                if (year != null)
                {
                    if (date.Year < year)
                    {
                        continue;
                    }
                    else if (date.Year > year)
                    {
                        break;
                    }
                }

                float? min = string.IsNullOrWhiteSpace(splitMin[1]) ? null : float.Parse(splitMin[1]);
                float? max = string.IsNullOrWhiteSpace(splitMax[1]) ? null : float.Parse(splitMax[1]);

                adjustedTemperatureRecords.Add(new TemperatureRecord(date, min, max));
            }

            return adjustedTemperatureRecords;
        }
    }
}
