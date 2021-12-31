using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.Core
{
    public static class TemperatureRecordExtensions
    {
        public static IEnumerable<IGrouping<short?, TemperatureRecord>> GroupByDays(this IEnumerable<TemperatureRecord> temperatureRecords, short numberOfDaysInGroup)
        {
            var numberOfDaysInYear = temperatureRecords.Validate();

            var remainder = numberOfDaysInYear % numberOfDaysInGroup;

            var grouping = temperatureRecords.GroupBy(x => 
                    x.Date.DayOfYear > numberOfDaysInYear - remainder
                                ? (short?)(numberOfDaysInYear / numberOfDaysInGroup - 1)
                                : (short)((x.Date.DayOfYear - 1) / numberOfDaysInGroup)
                                );
            return grouping;
        }

        /// <summary>
        /// Groups temperature records into groups of 7 days. The last group will have 8 days in it (or 9 days if it's a leap year).
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<IGrouping<short?, TemperatureRecord>> GroupByWeek(this IEnumerable<TemperatureRecord> temperatureRecords)
        {
            var weeklyGroups = temperatureRecords.GroupByDays(7);
            return weeklyGroups;
        }

        /// <summary>
        /// Groups temperature records into months.
        /// </summary>
        /// <param name="temperatureRecords"></param>
        /// <param name="validateRecords"></param>
        /// <returns></returns>
        public static IEnumerable<IGrouping<short?, TemperatureRecord>> GroupByMonth(this IEnumerable<TemperatureRecord> temperatureRecords, bool validateRecords = true)
        {
            if (validateRecords)
            {
                temperatureRecords.Validate();
            }

            var monthlyGroups = temperatureRecords.GroupBy(x => x.Month);
            return monthlyGroups;
        }

        public static int Validate(this IEnumerable<TemperatureRecord> values)
        {
            if (values.Any(x => x.Day == null || x.Month == null))
            {
                throw new NullReferenceException("Day and month values are required when grouping by week");
            }

            var year = values.First().Year;
            if (!values.All(x => x.Year == year))
            {
                throw new Exception($"All records need to be for the same year ({year})");
            }
            var calendar = new GregorianCalendar();
            var numberOfDaysInYear = calendar.GetDaysInYear(year);
            if (values.Count() != numberOfDaysInYear)
            {
                throw new Exception($"Year {year} needs {numberOfDaysInYear} temperature records for the year to be complete. You've only supplied {values.Count()} records");
            }

            if (!values.GroupBy(x => x.Date.DayOfYear).All(x => x.Count() == 1))
            {
                throw new Exception("All data must be for distinct dates");
            }

            return numberOfDaysInYear;
        }
    }
}
