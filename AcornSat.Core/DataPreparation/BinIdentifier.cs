using System;

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

        public override bool Equals(object obj)
        {
            BinIdentifier other = obj as BinIdentifier;

            if (other == null) return false;

            return other.Id == this.Id;
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

                    if (id.Contains("m"))
                    {
                        short month = (short)int.Parse(id.Substring(6, 2));

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

        public override bool Equals(object other)
        {
            return this._month == (other as MonthOnlyBinIdentifier)?._month;
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

    public class TropicalSeasonOnlyBinIdentifier : BinIdentifier
    {
        public TropicalSeasonOnlyBinIdentifier(TropicalSeasons season)
            : base(
                $"trops{season}",
                $"{season}")
        {
        }
    }
}
