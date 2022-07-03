using System;
using System.Globalization;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class DateHelpers
    {
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
            } while (d.Month == month);

            return d.AddDays(-1);
        }

        public class SouthernHemisphereTemperateSeasonOccurrence
        {
            public int Year;
            public SouthernHemisphereTemperateSeasons Season;

            public override bool Equals(object? obj)
            {
                var other = obj as SouthernHemisphereTemperateSeasonOccurrence;

                if (other == null) return false;

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

        public static SouthernHemisphereTemperateSeasonOccurrence GetSouthernHemisphereTemperateSeasonAndYear(short year, short month)
        {
            // Special case: December is Summer, and we place it in the next year's summer.
            if (month == 12) return new SouthernHemisphereTemperateSeasonOccurrence { Year = year + 1, Season = SouthernHemisphereTemperateSeasons.Summer };

            return new SouthernHemisphereTemperateSeasonOccurrence { Year = year, Season = GetSouthernHemisphereTemperateSeasonForMonth(month) };
        }

        public static SouthernHemisphereTemperateSeasons GetSouthernHemisphereTemperateSeasonForMonth(int month)
        {
            if (month <= 2 || month == 12) return SouthernHemisphereTemperateSeasons.Summer;
            if (month <= 5) return SouthernHemisphereTemperateSeasons.Autumn;
            if (month <= 8) return SouthernHemisphereTemperateSeasons.Winter;
            return SouthernHemisphereTemperateSeasons.Spring;
        }

        public static TropicalSeasons GetTropicalSeasonForMonth(int month)
        {
            if (month <= 4 || month >= 10) return TropicalSeasons.Wet;
            return TropicalSeasons.Dry;
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

        public struct DateOnlySpan
        {
            public DateOnly Start { get; set; }
            public DateOnly End { get; set; }
        }

        public static DateOnlySpan[] DivideDateSpanIntoSegmentsWithIncompleteFinalSegmentAddedToFinalSegment(DateOnly start, DateOnly end, int segmentSizeInDays)
        {
            int daysInSpan =
                (int)((end.ToDateTime(new TimeOnly()) - start.ToDateTime(new TimeOnly())).TotalDays) + 1;

            int completeSegmentsInSpan = daysInSpan / segmentSizeInDays;

            // Special case: if the requested segment size is bigger than the span we're dividing up, just return the whole span
            if (completeSegmentsInSpan == 0)
            {
                return new DateOnlySpan[]
                {
                    new DateOnlySpan { Start = start, End = end }
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
    }
}
