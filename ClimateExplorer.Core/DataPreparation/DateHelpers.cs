﻿namespace ClimateExplorer.Core.DataPreparation;

using System.Globalization;

public static class DateHelpers
{
    private const int FirstMonthOfSouthernHemisphereSummer = 12;
    private const int FirstMonthOfSouthernHemisphereWetSeason = 10;
    private const int LastMonthOfSouthernHemisphereWetSeason = 4;

    public static string GetShortMonthName(short monthNumber)
    {
        if (monthNumber < 1 || monthNumber > 12)
        {
            throw new ArgumentOutOfRangeException("monthNumber " + monthNumber);
        }

        return new DateTime(2000, monthNumber, 1).ToString("MMM", CultureInfo.InvariantCulture);
    }

    public static DateOnly GetLastDayInMonth(short year, short month)
    {
        // Strategy: start with the 28th day of the specified month (because no month has less than 28 days).
        // Keep adding days until we trip over into the next (which may also be the next year).
        DateOnly d = new DateOnly(year, month, 28);

        do
        {
            d = d.AddDays(1);
        }
        while (d.Month == month);

        return d.AddDays(-1);
    }

    public static SouthernHemisphereTemperateSeasonOccurrence GetSouthernHemisphereTemperateSeasonAndYear(short year, short month)
    {
        return
            new SouthernHemisphereTemperateSeasonOccurrence
            {
                // Special case: December is Summer, and we place it in the next year's summer.
                Year = year + ((month == 12) ? 1 : 0),
                Season = GetSouthernHemisphereTemperateSeasonForMonth(month),
            };
    }

    public static SouthernHemisphereTropicalSeasonOccurrence GetSouthernHemisphereTropicalSeasonAndYear(short year, short month)
    {
        return
            new SouthernHemisphereTropicalSeasonOccurrence
            {
                // Special case: Oct-Dec are wet season, and we place them in the next year's wet season.
                Year = year + ((month >= FirstMonthOfSouthernHemisphereWetSeason) ? 1 : 0),
                Season = GetSouthernHemisphereTropicalSeasonForMonth(month),
            };
    }

    public static SouthernHemisphereTemperateSeasons GetSouthernHemisphereTemperateSeasonForMonth(int month)
    {
        if (month <= 2 || month == 12)
        {
            return SouthernHemisphereTemperateSeasons.Summer;
        }

        if (month <= 5)
        {
            return SouthernHemisphereTemperateSeasons.Autumn;
        }

        if (month <= 8)
        {
            return SouthernHemisphereTemperateSeasons.Winter;
        }

        return SouthernHemisphereTemperateSeasons.Spring;
    }

    public static SouthernHemisphereTropicalSeasons GetSouthernHemisphereTropicalSeasonForMonth(int month)
    {
        if (month <= LastMonthOfSouthernHemisphereWetSeason || month >= FirstMonthOfSouthernHemisphereWetSeason)
        {
            return SouthernHemisphereTropicalSeasons.Wet;
        }

        return SouthernHemisphereTropicalSeasons.Dry;
    }

    public static DateOnly GetFirstDayInTemperateSeasonOccurrence(SouthernHemisphereTemperateSeasonOccurrence seasonOccurrence)
    {
        switch (seasonOccurrence.Season)
        {
            case SouthernHemisphereTemperateSeasons.Autumn: return new DateOnly(seasonOccurrence.Year, 3, 1);
            case SouthernHemisphereTemperateSeasons.Winter: return new DateOnly(seasonOccurrence.Year, 6, 1);
            case SouthernHemisphereTemperateSeasons.Spring: return new DateOnly(seasonOccurrence.Year, 9, 1);
            case SouthernHemisphereTemperateSeasons.Summer: return new DateOnly(seasonOccurrence.Year - 1, 12, 1);

            default: throw new NotImplementedException($"SouthernHemisphereTemperateSeason {seasonOccurrence}");
        }
    }

    public static DateOnly GetLastDayInTemperateSeasonOccurrence(SouthernHemisphereTemperateSeasonOccurrence seasonOccurrence)
    {
        switch (seasonOccurrence.Season)
        {
            case SouthernHemisphereTemperateSeasons.Summer: return new DateOnly(seasonOccurrence.Year, 3, 1).AddDays(-1);
            case SouthernHemisphereTemperateSeasons.Autumn: return new DateOnly(seasonOccurrence.Year, 5, 31);
            case SouthernHemisphereTemperateSeasons.Winter: return new DateOnly(seasonOccurrence.Year, 8, 31);
            case SouthernHemisphereTemperateSeasons.Spring: return new DateOnly(seasonOccurrence.Year, 11, 30);

            default: throw new NotImplementedException($"SouthernHemisphereTemperateSeason {seasonOccurrence}");
        }
    }

    public static DateOnly GetFirstDayInTropicalSeasonOccurrence(SouthernHemisphereTropicalSeasonOccurrence seasonOccurrence)
    {
        switch (seasonOccurrence.Season)
        {
            case SouthernHemisphereTropicalSeasons.Wet: return new DateOnly(seasonOccurrence.Year - 1, FirstMonthOfSouthernHemisphereWetSeason, 1);
            case SouthernHemisphereTropicalSeasons.Dry: return new DateOnly(seasonOccurrence.Year, LastMonthOfSouthernHemisphereWetSeason + 1, 1);

            default: throw new NotImplementedException($"SouthernHemisphereTropicalSeasonOccurrence {seasonOccurrence}");
        }
    }

    public static DateOnly GetLastDayInTropicalSeasonOccurrence(SouthernHemisphereTropicalSeasonOccurrence seasonOccurrence)
    {
        switch (seasonOccurrence.Season)
        {
            case SouthernHemisphereTropicalSeasons.Wet: return new DateOnly(seasonOccurrence.Year, LastMonthOfSouthernHemisphereWetSeason + 1, 1).AddDays(-1);
            case SouthernHemisphereTropicalSeasons.Dry: return new DateOnly(seasonOccurrence.Year, FirstMonthOfSouthernHemisphereWetSeason, 1).AddDays(-1);

            default: throw new NotImplementedException($"SouthernHemisphereTropicalSeasonOccurrence {seasonOccurrence}");
        }
    }

    public static int GetMonthIndex(DateOnly d)
    {
        return (d.Year * 12) + d.Month - 1;
    }

    public static DateOnly GetFirstDayInMonthByMonthIndex(int monthIndex)
    {
        return new DateOnly(monthIndex / 12, (monthIndex % 12) + 1, 1);
    }

    public static DateOnly GetLastDayInMonthByMonthIndex(int monthIndex)
    {
        var nextMonthIndex = monthIndex + 1;

        return new DateOnly(nextMonthIndex / 12, (nextMonthIndex % 12) + 1, 1).AddDays(-1);
    }

    public static DateOnlySpan[] DivideDateSpanIntoMonthSegments(DateOnly start, DateOnly end)
    {
        if (start.Day != 1)
        {
            throw new Exception($"Day component of start date must be 1 - {start}");
        }

        if (end.AddDays(1).Day != 1)
        {
            throw new Exception($"Day day of end date must be last day of month - {end}");
        }

        List<DateOnlySpan> spans = [];

        int firstMonthIndex = GetMonthIndex(start);
        int lastMonthIndex = GetMonthIndex(end);

        for (int monthIndex = firstMonthIndex; monthIndex <= lastMonthIndex; monthIndex++)
        {
            spans.Add(new DateOnlySpan(GetFirstDayInMonthByMonthIndex(monthIndex), GetLastDayInMonthByMonthIndex(monthIndex)));
        }

        return spans.ToArray();
    }

    public static DateOnlySpan[] DivideDateSpanIntoSegmentsWithIncompleteFinalSegmentAddedToFinalSegment(DateOnly start, DateOnly end, int segmentSizeInDays)
    {
        int daysInSpan =
            (int)(end.ToDateTime(default(TimeOnly)) - start.ToDateTime(default(TimeOnly))).TotalDays + 1;

        int completeSegmentsInSpan = daysInSpan / segmentSizeInDays;

        // Special case: if the requested segment size is bigger than the span we're dividing up, just return the whole span
        if (completeSegmentsInSpan == 0)
        {
            return new DateOnlySpan[]
            {
                new DateOnlySpan { Start = start, End = end },
            };
        }

        DateOnlySpan[] result = new DateOnlySpan[completeSegmentsInSpan];

        DateOnly runningStart = start;

        for (int i = 0; i < completeSegmentsInSpan; i++)
        {
            result[i].Start = runningStart;
            result[i].End = runningStart.AddDays(segmentSizeInDays - 1);

            runningStart = runningStart.AddDays(segmentSizeInDays);
        }

        result[completeSegmentsInSpan - 1].End = end;

        return result;
    }

    public static int CountDaysInRange(DateOnly start, DateOnly end)
    {
        return (int)(end.ToDateTime(default(TimeOnly)) - start.ToDateTime(default(TimeOnly))).TotalDays + 1;
    }

    public static DateOnly ConvertDecimalDate(double decimalDate)
    {
        var year = (int)double.Floor(decimalDate);
        var reminder = decimalDate - year;
        var daysPerYear = DateTime.IsLeapYear(year) ? 366 : 365;
        var milliseconds = reminder * daysPerYear * 24 * 60 * 60 * 1000D;
        var yearDate = new DateTime(year, 1, 1);
        return DateOnly.FromDateTime(yearDate.AddMilliseconds(milliseconds));
    }

    public struct DateOnlySpan
    {
        public DateOnlySpan(DateOnly start, DateOnly end)
        {
            Start = start;
            End = end;
        }

        public DateOnly Start { get; set; }
        public DateOnly End { get; set; }
    }

    public class SouthernHemisphereTropicalSeasonOccurrence
    {
        public int Year { get; set; }
        public SouthernHemisphereTropicalSeasons Season { get; set; }

        public override bool Equals(object? obj)
        {
            var other = obj as SouthernHemisphereTropicalSeasonOccurrence;

            if (other == null)
            {
                return false;
            }

            return Year == other.Year && Season == other.Season;
        }

        public override int GetHashCode()
        {
            return Year * (((int)Season) + 10);
        }

        public override string ToString()
        {
            return $"{Season} {Year}";
        }
    }

    public class SouthernHemisphereTemperateSeasonOccurrence
    {
        public int Year { get; set; }
        public SouthernHemisphereTemperateSeasons Season { get; set; }

        public override bool Equals(object? obj)
        {
            var other = obj as SouthernHemisphereTemperateSeasonOccurrence;

            if (other == null)
            {
                return false;
            }

            return Year == other.Year && Season == other.Season;
        }

        public override int GetHashCode()
        {
            return Year * (((int)Season) + 10);
        }

        public override string ToString()
        {
            return $"{Season} {Year}";
        }
    }
}
