using System;

namespace ClimateExplorer.Core.DataPreparation
{
    public class Cup
    {
        public DateOnly FirstDayInCup { get; set; }
        public DateOnly LastDayInCup { get; set; }
        public TemporalDataPoint[] DataPoints { get; set; }
        public int DaysInCup
        {
            get
            {
                return (int)((LastDayInCup.ToDateTime(new TimeOnly()) - FirstDayInCup.ToDateTime(new TimeOnly())).TotalDays) + 1;
            }
        }

        public override string ToString()
        {
            return FirstDayInCup.ToString("yyyy-MM-dd") + " -> " + LastDayInCup.ToString("yyyy-MM-dd") + " (" + DataPoints.Length + " data points / " + DaysInCup + " days)";
        }
    }
}
