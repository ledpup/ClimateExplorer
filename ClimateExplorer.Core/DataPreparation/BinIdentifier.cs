using System.Globalization;

namespace ClimateExplorer.Core.DataPreparation
{
    public abstract class BinIdentifier : IComparable<BinIdentifier>
    {
        public string Id { get; private set; }
        public string Label { get; private set; }

        public BinIdentifier(string id, string label)
        {
            Id = id;
            Label = label;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not BinIdentifier other)
            {
                return false;
            }

            return other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return "Bin " + Label;
        }

        public static BinIdentifier Parse(string id)
        {
            try
            {
                if (id == null)
                {
                    throw new ArgumentNullException("id");
                }

                if (id.StartsWith("y"))
                {
                    short year = (short)int.Parse(id.Substring(1, 4));

                    if (id.Contains("w"))
                    {
                        short week = (short)int.Parse(id.Substring(6, 2));

                        return new YearAndWeekBinIdentifier(year, week);
                    }

                    if (id.Contains("m"))
                    {
                        short month = (short)int.Parse(id.Substring(6, 2));

                        if (id.Contains("d"))
                        {
                            short day = (short)int.Parse(id.Substring(9, 2));

                            return new YearAndDayBinIdentifier(year, month, day);
                        }

                        return new YearAndMonthBinIdentifier(year, month);
                    }
                    else
                    {
                        return new YearBinIdentifier(year);
                    }
                }

                if (id.StartsWith("m"))
                {
                    short month = (short)int.Parse(id.Substring(1));

                    return new MonthOnlyBinIdentifier(month);
                }

                throw new NotImplementedException("Bin identifier id " + id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse identifier {id}", ex);
            }
        }

        public int CompareTo(BinIdentifier? other)
        {
            if (this is BinIdentifierForGaplessBin b1 &&
                other is BinIdentifierForGaplessBin b2)
            {
                return b1.CompareTo(b2);
            }

            if (this is MonthOnlyBinIdentifier m1 &&
                other is MonthOnlyBinIdentifier m2)
            {
                return m1.CompareTo(m2);
            }

            throw new NotImplementedException();
        }
    }

    public abstract class BinIdentifierForGaplessBin : BinIdentifier, IComparable<BinIdentifierForGaplessBin>
    {
        public DateOnly FirstDayInBin { get; private set; }
        public DateOnly LastDayInBin { get; private set; }

        public BinIdentifierForGaplessBin(string id, string label, DateOnly firstDayInBin, DateOnly lastDayInBin)
            : base(id, label)
        {
            FirstDayInBin = firstDayInBin;
            LastDayInBin = lastDayInBin;
        }

        public int CompareTo(BinIdentifierForGaplessBin? other)
        {
            return this.FirstDayInBin.CompareTo(other?.FirstDayInBin);
        }
    }

    public class YearBinIdentifier : BinIdentifierForGaplessBin
    {
        short _year;

        public YearBinIdentifier(short year) 
            : base(
                $"y{year}",
                $"{year}",
                new DateOnly(year, 1, 1),
                new DateOnly(year, 12, 31))
        {
            _year = year;
        }

        public short Year => _year;

        public IEnumerable<YearBinIdentifier> EnumerateYearBinRangeUpTo(YearBinIdentifier endOfRange)
        {
            for (short i = _year; i <= endOfRange.Year; i++)
            {
                yield return new YearBinIdentifier(i);
            }
        }
    }

    public class YearAndMonthBinIdentifier : BinIdentifierForGaplessBin
    {
        short _year;
        short _month;

        public YearAndMonthBinIdentifier(short year, short month)
            : base(
                  $"y{year}m{month.ToString().PadLeft(2, '0')}", 
                  $"{DateHelpers.GetShortMonthName(month)} {year}",
                  new DateOnly(year, month, 1),
                  DateHelpers.GetLastDayInMonth(year, month))
        {
            _year = year;
            _month = month;
        }

        public short Year => _year;
        public short Month => _month;

        public IEnumerable<YearAndMonthBinIdentifier> EnumerateYearAndMonthBinRangeUpTo(YearAndMonthBinIdentifier endOfRange)
        {
            for (int i = _year * 12 + _month - 1; i <= endOfRange.Year * 12 + endOfRange.Month - 1; i++)
            {
                yield return new YearAndMonthBinIdentifier((short)(i / 12), (short)((i % 12) + 1));
            }
        }
    }

    public class YearAndWeekBinIdentifier : BinIdentifierForGaplessBin
    {
        short _year;
        short _week;

        public YearAndWeekBinIdentifier(short year, short week)
            : base(
                  $"y{year}w{week.ToString().PadLeft(2, '0')}",
                  $"Week {week} {year}",
                  FirstDateOfWeekIso8601(year, week),
                  FirstDateOfWeekIso8601(year, week).AddDays(6))
        {
            _year = year;
            _week = week;
        }

        public short Year => _year;
        public short Week => _week;

        public IEnumerable<YearAndWeekBinIdentifier> EnumerateYearAndWeekBinRangeUpTo(YearAndWeekBinIdentifier endOfRange)
        {
            for (int i = _year * 52 + _week - 1; i <= endOfRange.Year * 52 + endOfRange.Week - 1; i++)
            {
                yield return new YearAndWeekBinIdentifier((short)(i / 52), (short)((i % 52) + 1));
            }
        }

        public static DateOnly FirstDateOfWeekIso8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return DateOnly.FromDateTime(result.AddDays(-3));
        }
    }

    public class YearAndDayBinIdentifier : BinIdentifierForGaplessBin
    {
        short _year;
        short _month;
        short _day;

        public YearAndDayBinIdentifier(short year, short month, short day)
            : base(
                  $"y{year}m{month.ToString().PadLeft(2, '0')}d{day.ToString().PadLeft(2, '0')}",
                  $"{day} {DateHelpers.GetShortMonthName(month)} {year}",
                  new DateOnly(year, month, day),
                  new DateOnly(year, month, day))
        {
            _year = year;
            _month = month;
            _day = day;
        }

        public short Year => _year;
        public short Month => _month;
        public short Day => _day;

        public IEnumerable<YearAndDayBinIdentifier> EnumerateYearAndDayBinRangeUpTo(YearAndDayBinIdentifier endOfRange)
        {
            var from = new DateOnly(Year, Month, Day);
            var to = new DateOnly(endOfRange.Year, endOfRange.Month, endOfRange.Day);

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                yield return new YearAndDayBinIdentifier((short)day.Year, (short)day.Month, (short)day.Day);
            }
        }
    }

    public class MonthOnlyBinIdentifier : BinIdentifier, IComparable<MonthOnlyBinIdentifier>
    {
        int _month;

        public MonthOnlyBinIdentifier(short month)
            : base(
                $"m{month}",
                $"{DateHelpers.GetShortMonthName(month)}")
        {
            _month = month;
        }

        public int Month { get { return _month; } }

        public int CompareTo(MonthOnlyBinIdentifier? other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            return this._month - other._month;
        }

        public override bool Equals(object? other)
        {
            return this._month == (other as MonthOnlyBinIdentifier)?._month;
        }

        public override int GetHashCode()
        {
            return _month.GetHashCode();
        }
    }

    public class SouthernHemisphereTemperateSeasonOnlyBinIdentifier : BinIdentifier
    {
        public SouthernHemisphereTemperateSeasonOnlyBinIdentifier(SouthernHemisphereTemperateSeasons season)
            : base(
                $"temps{season}",
                $"{season}")
        {
        }
    }

    public class SouthernHemisphereTropicalSeasonOnlyBinIdentifier : BinIdentifier
    {
        public SouthernHemisphereTropicalSeasonOnlyBinIdentifier(SouthernHemisphereTropicalSeasons season)
            : base(
                $"trops{season}",
                $"{season}")
        {
        }
    }
}
