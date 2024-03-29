﻿namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using System.Globalization;

public static class Extensions
{
    public static IEnumerable<IGrouping<short?, DataRecord>> GroupYearByDays(this IEnumerable<DataRecord> temperatureRecords, short numberOfDaysInGroup)
    {
        var numberOfDaysInYear = temperatureRecords.ValidateDailySingleYear();

        var remainder = numberOfDaysInYear % numberOfDaysInGroup;

        var grouping = temperatureRecords.GroupBy(x =>
                x.Date!.Value.DayOfYear > numberOfDaysInYear - remainder
                            ? (short?)((numberOfDaysInYear / numberOfDaysInGroup) - 1)
                            : (short)((x.Date.Value.DayOfYear - 1) / numberOfDaysInGroup));
        return grouping;
    }

    public static IEnumerable<IGrouping<short?, DataRecord>> GroupYearByWeek(this IEnumerable<DataRecord> temperatureRecords)
    {
        var weeklyGroups = temperatureRecords.GroupYearByDays(7);
        return weeklyGroups;
    }

    public static IEnumerable<IGrouping<short?, DataRecord>> GroupYearByMonth(this IEnumerable<DataRecord> temperatureRecords, bool validateRecords = true)
    {
        if (validateRecords)
        {
            temperatureRecords.ValidateDailySingleYear();
        }

        var monthlyGroups = temperatureRecords.GroupBy(x => x.Month);
        return monthlyGroups;
    }

    public static int ValidateDailySingleYear(this IEnumerable<DataRecord> values)
    {
        if (values.Any(x => x.Day == null || x.Month == null))
        {
            throw new NullReferenceException("All day and month values are required");
        }

        var year = values.First().Year;
        if (!values.All(x => x.Year == year))
        {
            throw new Exception($"All records need to be for the same year ({year})");
        }

        var calendar = new GregorianCalendar();
        var numberOfDaysInYear = calendar.GetDaysInYear(year);

        if (!values.GroupBy(x => x.Date!.Value.DayOfYear).All(x => x.Count() == 1))
        {
            throw new Exception("All data must be for distinct dates");
        }

        if (values.Count() != numberOfDaysInYear)
        {
            throw new Exception($"Only {values.Count()} days of data in a year that has {numberOfDaysInYear} days");
        }

        return values.Count();
    }

    public static void ValidateDaily(this IEnumerable<DataRecord> values)
    {
        if (values.Any(x => x.Day == null || x.Month == null))
        {
            throw new NullReferenceException("All day and month values are required");
        }

        var calendar = new GregorianCalendar();
        var invalidData = values.GroupBy(x => x.Year).Where(x => x.Count() > calendar.GetDaysInYear(x.Key));
        if (invalidData.Any())
        {
            throw new Exception($"Data is invalid. More than a year worth of records for the years {string.Join(", ", invalidData.Select(x => x.Key))}");
        }

        var duplicateDates = values.GroupBy(x => x.Date)
                                          .Where(x => x.Count() > 1)
                                          .Select(x => x.Key)
                                          .ToList();
        if (duplicateDates.Any())
        {
            throw new Exception($"There are duplicate dates ({string.Join(", ", duplicateDates.Select(x => x!.Value.ToShortDateString()))}. The file is corrupt.");
        }
    }

    public static void ValidateMonthly(this IEnumerable<DataRecord> values)
    {
        if (values.Any(x => x.Day != null))
        {
            throw new Exception("No day values are permitted for monthly data");
        }

        if (values.Any(x => x.Month == null))
        {
            throw new NullReferenceException("All month values are required");
        }

        var invalidData = values.GroupBy(x => x.Year).Where(x => x.Count() > 12);
        if (invalidData.Any())
        {
            throw new Exception($"No more than 12 records per year is permitted.");
        }

        var duplicateMonths = values.GroupBy(x => new { x.Year, x.Month })
                                          .Where(x => x.Count() > 1)
                                          .Select(x => x.Key)
                                          .ToList();
        if (duplicateMonths.Any())
        {
            throw new Exception($"There are duplicate dates ({string.Join(", ", duplicateMonths)}. The file is corrupt.");
        }
    }

    public static string ToLowerFirstChar(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToLower(input[0]) + input.Substring(1);
    }

    public static string Truncate(this string value, int maxChars)
    {
        return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
    }
}
