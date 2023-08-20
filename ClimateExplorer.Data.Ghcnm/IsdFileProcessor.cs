using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClimateExplorer.Data.Ghcnm;

internal class IsdFileProcessor
{
    internal static Dictionary<DateOnly, List<TimedRecord>> Transform(string[] records, ILogger<Program> logger)
    {
        var mandatoryDataRegex = @"^(\d{4})(?<catalogId>\d{6})(?<wbanId>\d{5})
(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})(?<time>\d{4})
(?<sourceDataFlag>\d{1})
(?<latitude>[\-|\+]+\d{5})(?<longitude>[\-|\+]+\d{6})
(?<reportType>.{5})
(?<elevation>[\-|\+]+\d{4})(?<callLetterId>.{5})(?<processName>.{4})
(?<windDirection>\d{3})(?<windDirectionQuality>\d)(?<windDirectionType>\w)(?<windSpeed>\d{4})(?<windSpeedQuality>.)
(?<ceilingHeight>\d{5})(?<ceilingQuality>\d)(?<ceilingDeterminationCode>.)(?<cavokCode>.)
(?<horizontalVisibility>\d{6})(?<horizontalVisibilityQuality>\d)(?<horizontalVisibilityVariability>.)(?<horizontalVisVariabilityQuality>\d)
(?<temperature>[\-|\+]+\d{4})(?<temperatureQuality>.)
(?<dewPointTemperature>[\-|\+]+\d{4})(?<dewPointQuality>.)
(?<airPressure>\d{5})(?<airPressureQuality>\d{1})
(?<additionalDataId>ADD)?.*$"
    .Replace("\r\n", string.Empty);

        var regEx = new Regex(mandatoryDataRegex);

        var dailyRecords = new Dictionary<DateOnly, List<TimedRecord>>();

        foreach (var line in records)
        {
            var match = regEx.Match(line);
            if (!match.Success)
            {
                if (line.StartsWith("Failed"))
                {
                    break;
                }
                logger.LogError($"Line {Array.IndexOf(records, line) + 1}: regular expression does not match the data in line {line}");
                logger.LogWarning($"Skipping line {Array.IndexOf(records, line) + 1}");
                continue;
            }
            var groups = regEx.Match(line).Groups;
            var catalogId = groups["catalogId"];
            var dateOnly = new DateOnly(Convert.ToInt16(groups["year"].Value), Convert.ToInt16(groups["month"].Value), Convert.ToInt16(groups["day"].Value));

            var timeString = groups["time"].Value;
            if (timeString == "2400")
            {
                logger.LogError($"Line {Array.IndexOf(records, line) + 1}: time is recorded as 2400. 2400 is not a time in .NET. Changing it to 23:59");
                timeString = "2359";
            }
            var timeOnly = new TimeOnly(Convert.ToInt16(timeString.Substring(0, 2)), Convert.ToInt16(timeString.Substring(2, 2)));

            if (!dailyRecords.ContainsKey(dateOnly))
            {
                dailyRecords.Add(dateOnly, new List<TimedRecord>());
            }

            var timedRecord = new TimedRecord(timeOnly)
            {
                ReportType = groups["reportType"].Value
            };

            if (dailyRecords[dateOnly].Any(x => x.Time == timeOnly && x.ReportType == groups["reportType"].Value))
            {
                logger.LogWarning($"Line {Array.IndexOf(records, line) + 1}: Already have TimedRecord on {dateOnly} at the time {timeOnly} with the ReportType {dailyRecords[dateOnly].First().ReportType}. Will skip mandatory data and only look for ADD records.");
            }
            else
            {
                float? temp = Convert.ToSingle(groups["temperature"].Value) / 10f;
                if (groups["temperature"].Value == "+9999"
                    || groups["temperatureQuality"].Value == "2"  // Suspect
                    || groups["temperatureQuality"].Value == "3"  // Erroneous
                    || groups["temperatureQuality"].Value == "6"  // Suspect
                    || groups["temperatureQuality"].Value == "7") // Erroneous
                {
                    temp = null;
                }
                timedRecord.DataRecords.Add(new DataRecord() { Type = DataType.Temperature, Value = temp });

                float? dewPointTemperature = Convert.ToSingle(groups["dewPointTemperature"].Value) / 10f;
                if (groups["dewPointTemperature"].Value == "+9999"
                    || groups["dewPointQuality"].Value == "2"  // Suspect
                    || groups["dewPointQuality"].Value == "3"  // Erroneous
                    || groups["dewPointQuality"].Value == "6"  // Suspect
                    || groups["dewPointQuality"].Value == "7") // Erroneous
                {
                    dewPointTemperature = null;
                }
                timedRecord.DataRecords.Add(new DataRecord() { Type = DataType.DewPointTemperature, Value = dewPointTemperature });
            }

            var mandatoryAdditionalSplit = line.Split("ADD");
            if (mandatoryAdditionalSplit.Length > 2)
            {
                logger.LogWarning($"Expecting only two parts of the data, mandatory and additional data. There are {mandatoryAdditionalSplit.Length} parts for this line. Line is: {line}");
            }
            
            if (mandatoryAdditionalSplit.Length >= 2)
            {
                var additionalData = mandatoryAdditionalSplit[1];
                var rainfall = RainfallRecords(additionalData);
                if (rainfall != 0)
                {
                    timedRecord.DataRecords.Add(new DataRecord() { Type = DataType.Rainfall, Value = rainfall });
                }
            }

            if (timedRecord.DataRecords.Any())
            {
                dailyRecords[dateOnly].Add(timedRecord);
            }
        }

        return dailyRecords;
    }

    private static float? RainfallRecords(string additionalData)
    {
        // Rainfall example: AA106000091AA224999999AA399004091
        float dailyDepth = 0;
        for (var i = 1; i <= 4; i++)
        {
            var expression = "AA" + i + @"(?<period>\d{2})(?<depth>\d{4})(?<condition>\d)(?<quality>\d)";

            var regEx = new Regex(expression);

            if (regEx.IsMatch(additionalData))
            {
                var groups = regEx.Match(additionalData).Groups;

                if ((groups["depth"].Value == "9999"
                        || groups["quality"].Value == "2"  // Suspect
                        || groups["quality"].Value == "3"  // Erroneous
                        || groups["quality"].Value == "6"  // Suspect
                        || groups["quality"].Value == "7") // Erroneous
                        )
                {
                    continue;
                }

                var depth = Convert.ToSingle(groups["depth"].Value) / 10f;
                dailyDepth += depth;
            }
            else
            {
                // If we haven't found rainfall for i, we won't find any others
                break;
            }
        }

        return dailyDepth;
    }
}
