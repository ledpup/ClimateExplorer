using System;

namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public abstract class BinIdentifier
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
    }


    public abstract class BinIdentifierForGaplessBin : BinIdentifier
    {
        public DateOnly FirstDayInBin { get; private set; }
        public DateOnly LastDayInBin { get; private set; }

        public BinIdentifierForGaplessBin(string id, string label, DateOnly firstDayInBin, DateOnly lastDayInBin)
            : base(id, label)
        {
            FirstDayInBin = firstDayInBin;
            LastDayInBin = lastDayInBin;
        }
    }

    public class YearBinIdentifier : BinIdentifierForGaplessBin
    {
        public YearBinIdentifier(short year) 
            : base(
                $"y{year}",
                $"{year}",
                new DateOnly(year, 1, 1),
                new DateOnly(year, 12, 31))
        {
        }
    }

    public class YearAndMonthBinIdentifier : BinIdentifierForGaplessBin
    {
        public YearAndMonthBinIdentifier(short year, short month)
            : base(
                  $"y{year}m{month.ToString().PadLeft(2, '0')}", 
                  $"{DateHelpers.GetShortMonthName(month)} {year}",
                  new DateOnly(year, month, 1),
                  DateHelpers.GetLastDayInMonth(year, month))
        {
        }
    }

    public class MonthOnlyBinIdentifier : BinIdentifier
    {
        public MonthOnlyBinIdentifier(short month)
            : base(
                $"m{month}",
                $"{DateHelpers.GetShortMonthName(month)}")
        {
        }
    }

    public class TemperateSeasonOnlyBinIdentifier : BinIdentifier
    {
        public TemperateSeasonOnlyBinIdentifier(TemperateSeasons season)
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
